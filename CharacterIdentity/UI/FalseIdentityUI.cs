using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using CharacterAppearance;
using ExtensionsPlugin;
using NLog;
using NuiMVC;

namespace CharacterIdentity.UI
{
    
    internal sealed class FalseIdentityUI
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private enum State : byte { Select, Create, Edit, PickPortrait, ConfirmDelete, EditAppearance }

        private State state = State.Select;

        private readonly NwPlayer _player;
        private readonly NwCreature _pc;
        private readonly Guid _guid;


        private IdentityInfo[] infos;

        private IdentityInfo[] falseIdentities;
        private CharacterIdentityState identityState;


        private Model.IdentitySelector? identitySelectorModel;
        private Controller.IdentitySelector? identitySelectorController;

        private Model.IdentityEditor? identityEditorModel;
        private Controller.IdentityEditor? identityEditorController;

        private Controller.PortraitPicker? portraitPickerController;

        public FalseIdentityUI(NwPlayer player, IdentityInfo[] infos, CharacterIdentityState identityState)
        {
            _player = player;
            _pc = player.LoginCreature!;
            _guid = _pc.UUID;



            // using EventCallbackType.Before causes segmentation fault.
            IdentityManager.EventService.Subscribe<OnClientLevelUpBegin, OnClientLevelUpBegin.Factory>(_pc, OnPCLevelUpBegin, EventCallbackType.After);

            _pc.OnLevelDown += OnPCLevelDown;


            this.identityState = identityState;
            this.infos = infos;

            falseIdentities = infos.Where(i => i.ID != identityState.TrueIdentity.ID).ToArray();

            OpenIdentitySelector();
        }


        void OnPCLevelDown(OnLevelDown _) => ForceClose();
        void OnPCLevelUpBegin(OnClientLevelUpBegin _) => ForceClose();


        private static readonly HashSet<FalseIdentityUI> _instances = new();
        public static void Open(NwPlayer player)
        {
            if (!player.IsValid || player.IsDM) return;

            var pc = player.ControlledCreature;

            if (pc == null || !pc.IsValid || pc != player.LoginCreature) return;

            var validClasses = CharacterIdentityService.ServiceConfig.RequiredClassLevels;

            bool skipClassCheck = pc.IsGypsy();
            
            foreach (var c in pc.Classes)
            {
                int req = 0;
                if (skipClassCheck || validClasses.TryGetValue(c.Class.Id.ToString(), out req) || validClasses.TryGetValue(c.Class.ClassType.ToString(), out req))
                {
                    if (skipClassCheck || c.Level >= req)
                    {
                        var idState = CharacterIdentityState.GetState(player)
                        ?? throw new InvalidOperationException($"Every player needs to have a {nameof(CharacterIdentityState)} object attached.");

                        if (!IdentityManager.TryGetCharacterIdentitityInfosFromDatabase(pc.UUID, out var infos, out var trueId, out var activeId))
                        {
                            player.SendServerMessage("Błąd, nie udało się wczytać tożsamości.", ColorConstants.Red);
                            _log.Error("Failed to get identities of character " + pc.Name);
                            return;
                        }
                        else if (idState.ActiveIdentity.ID != activeId)
                        {
                            player.SendServerMessage("Błąd, aktywna tożsamość w bazie danych różni się od aktywnej tożsamości na serwerze", ColorConstants.Red);
                            _log.Error($"Active identity mismatch: {idState.ActiveIdentity.ID} (game), {activeId} (database), pcGuid: {pc.UUID}");
                            return;
                        }
                        else if (idState.TrueIdentity.ID != trueId)
                        {
                            player.SendServerMessage("Błąd, prawdziwa tożsamość w bazie danych różni się od aktywnej tożsamości na serwerze", ColorConstants.Red);
                            _log.Error($"True identity mismatch: {idState.TrueIdentity.ID} (game), {trueId} (database), pcGuid: {pc.UUID}");
                            return;
                        }
                        else if (infos.Count(i => i.ID == trueId) != 1)
                        {
                            player.SendServerMessage("Błąd, postać musi posiadać dokładnie jedną prawdziwą tożsamość.", ColorConstants.Red);
                            _log.Error("None or multiple true identities");
                            return;
                        }
                        else if(infos.Count(i=>i.ID == activeId) != 1)
                        {
                            player.SendServerMessage("Błąd, postać musi posiadać dokładnie jedną aktywną tożsamość.", ColorConstants.Red);
                            _log.Error("None or multiple active identities");
                            return;
                        }
                        _instances.Add(new(player, infos, idState));

                        return;
                    }
                }
            }

            player.SendServerMessage("Nie spełniasz wymogów do kreowania fałszywych tożsamości.", ColorConstants.Red);
        }

        void ForceClose()
        {
            // if (deletePrompt != null)
            // {
            //     deletePrompt.ClosedEvent -= OnDeletePromptClosed;
            //     deletePrompt.Close();
            //     deletePrompt = null;
            // }

            if (portraitPickerController != null)
            {
                portraitPickerController.ClosedEvent -= OnPortraitPickerClosed;
                portraitPickerController.Close();
                portraitPickerController = null;
            }

            if (identityEditorController != null)
            {
                identityEditorController.ClosedEvent -= OnIdentityEditorClosed;
                identityEditorController.PortraitOpen -= OpenPortraitPicker;
                identityEditorController.Close();
                identityEditorController = null;
            }

            if (identitySelectorController != null)
            {
                identitySelectorController.ClosedEvent -= OnIdentitySelectorClosed;
                identitySelectorController.EditorOpen -= OpenIdentityEditor;
                identitySelectorController.IdentityChanged -= OnIdentityChanged;
                identitySelectorController.IdentityDeleted -= OnIdentityDeleted;
                identitySelectorController.Close();
                identitySelectorController = null;
            }

            IdentityManager.EventService.Unsubscribe<OnClientLevelUpBegin, OnClientLevelUpBegin.Factory>(_pc, OnPCLevelUpBegin, EventCallbackType.After);
            _pc.OnLevelDown -= OnPCLevelDown;
            
            if (!_instances.Remove(this)) throw new InvalidOperationException($"Instance of {nameof(FalseIdentityUI)} is not present in the HashSet");
        }


        private Model.IdentitySelector CreateSelectorModel()
        {
            int maxIdentities = IdentityManager.GetMaxIdentities(_pc);

            falseIdentities = infos.Where(i => i.ID != identityState.TrueIdentity.ID).Take(maxIdentities).ToArray();

            return identitySelectorModel = new(_player, falseIdentities, identityState.TrueIdentity, identityState.ActiveIdentity.ID);
        }
        

        private void OpenIdentitySelector()
        {
            state = State.Select;

            identitySelectorModel ??= CreateSelectorModel();

            identitySelectorController = new(_player, identitySelectorModel);

            identitySelectorController.EditorOpen += OpenIdentityEditor;
            identitySelectorController.IdentityChanged += OnIdentityChanged;
            identitySelectorController.IdentityDeleted += OnIdentityDeleted;
            identitySelectorController.ClosedEvent += OnIdentitySelectorClosed;
        }

        void OnIdentitySelectorClosed(ControllerBase controller, object? data)
        {
            if (controller is not UI.Controller.IdentitySelector)
                _log.Error($"Handler called with controller which is not {nameof(UI.Controller.IdentitySelector)}", nameof(controller));

            ForceClose();
        }

        void OnIdentityChanged()
        {
            var newFalseIdentity = identitySelectorModel!.CurrentIdentity == identityState.TrueIdentity ? null : identitySelectorModel.CurrentIdentity;
            
            identityState.SetFalseIdentity(newFalseIdentity);
        }

        void OnIdentityDeleted(int identityID)
        {
            if (!IdentityManager.DeleteIdentity(identityID))
            {
                _player.SendServerMessage("Błąd serwera. Nie udało się usunąć fałszywej tożsamości", ColorConstants.Red);
                _log.Error($"Failed to delete identity of player {_player.PlayerName}'s character \'{_pc.OriginalName}\' ({identityID})");
                ForceClose();
                return;
            }

            if (!IdentityManager.TryGetCharacterIdentitityInfosFromDatabase(_guid, out var infos, out _, out _))
            {
                _player.SendServerMessage("Błąd serwera. Nie udało się odświeżyć listy tożsamości", ColorConstants.Red);
                _log.Error($"Failed to get updated list of player {_player.PlayerName}'s character \'{_pc.OriginalName}\' identities from database");
                ForceClose();
                return;
            }

            this.infos = infos;

            identitySelectorController!.SetModel(CreateSelectorModel());
        }



        private void OpenIdentityEditor(bool createNew)
        {
            switch (state)
            {

                case State.Select:
                    if (identityState.IsPolymorphed)
                    {
                        _player.SendServerMessage("Nie można edytować tożsamości będąc pod wpływem polimorfii.", ColorConstants.Red);
                        ForceClose();
                        return;
                    }

                    state = createNew ? State.Create : State.Edit;

                    // close the selector window without handling ClosedEvent (stateful selector model is still stored in memory)
                    identitySelectorController!.ClosedEvent -= OnIdentitySelectorClosed;
                    identitySelectorController.EditorOpen -= OpenIdentityEditor;
                    identitySelectorController.IdentityChanged -= OnIdentityChanged;
                    identitySelectorController.IdentityDeleted -= OnIdentityDeleted;
                    identitySelectorController.Close();
                    identitySelectorController = null;

                    // open a new editor window. Empty identity will trigger 'create new' window to open, otherwise view be filled with selected identity's data
                    identityEditorModel = new(_pc, createNew ? Identity.Empty : identitySelectorModel!.SelectedIdentity.Identity);
                    identityEditorController = new(_player, identityEditorModel);
                    identityEditorController.PortraitOpen += OpenPortraitPicker;
                    identityEditorController.ClosedEvent += OnIdentityEditorClosed;
                    break;


                case State.EditAppearance: throw new NotImplementedException("Appearance editor is not implemented yet");


                default:
                    _log.Error("Invalid state " + state.ToString());
                    ForceClose();
                    return;
            }
        }

        void OnIdentityEditorClosed(ControllerBase controller, object? data)
        {
            if (data is not Identity identity)
                throw new ArgumentException("IdentityEditor controller must return object of Identity type", nameof(data));

            if (controller is not Controller.IdentityEditor editor)
                throw new ArgumentException($"Handler called with controller which is not {nameof(UI.Controller.IdentityEditor)}", nameof(controller));

            switch (state)
            {
                case State.PickPortrait:

                    state = identityEditorModel!.IsCreatingNew ? State.Create : State.Edit;

                    portraitPickerController!.ClosedEvent -= OnPortraitPickerClosed;
                    portraitPickerController.Close();
                    portraitPickerController = null;

                    OnIdentityEditorClosed(editor, identity);
                    return;

                case State.Edit:
                    {
                        if (identity.IsEmpty)
                            break;

                        var info = identitySelectorModel!.SelectedIdentity;

                        if (info.ID == identityState.TrueIdentity.ID || info == identitySelectorModel.TrueIdentity) throw new InvalidOperationException("IdentityEditor has been opened on a true identity!");


                        var oldIdentity = info.Identity;

                        info.Identity = identity;

                        if (!IdentityManager.TryUpdateIdentityInfoInDatabase(info))
                        {
                            info.Identity = oldIdentity;
                            _player.SendServerMessage("Nie udało się nadpisać istniejącej tożsamości", ColorConstants.Red);
                            _log.Error($"Failed to override existing identity for {_player.PlayerName}'s character \'{_pc.OriginalName}\' ({info.Name} : {info.ID})");
                        }

                        break;
                    }

                case State.Create:
                    {
                        if (identity.IsEmpty)
                            break;

                        if (IdentityManager.FindExactIdentity(_guid, identity))
                            _player.SendServerMessage("Taka tożsamość już istnieje.");
                        else if (!IdentityManager.TrySaveCharacterIdentityInDatabase(_guid, identity, false, out var info))
                        {
                            _player.SendServerMessage("Błąd serwera. Nie udało się zapisać nowej tożsamości", ColorConstants.Red);
                            _log.Error($"Failed to save new identity of player {_player.PlayerName}'s character \'{_pc.OriginalName}\' ({identity.FirstName + ' ' + identity.LastName})");
                            ForceClose();
                            break;
                        }
                        
                        if (!IdentityManager.TryGetCharacterIdentitityInfosFromDatabase(_guid, out var infos, out _, out _))
                        {
                            _player.SendServerMessage("Błąd serwera. Nie udało się odświeżyć listy tożsamości", ColorConstants.Red);
                            _log.Error($"Failed to get updated list of player {_player.PlayerName}'s character \'{_pc.OriginalName}\' identities from database");
                            ForceClose();
                            break;
                        }
                        else
                        {
                            this.infos = infos;
                            identitySelectorModel = null; // need to re-create the model with new list of identities fetched from database to ensure its synchronized with ui
                        }
                        break;
                    }

                default:
                    _log.Error("Invalid state " + state.ToString());
                    ForceClose();
                    return;
            }

            editor.ClosedEvent -= OnIdentityEditorClosed;
            editor.PortraitOpen -= OpenPortraitPicker;
            identityEditorController = null;
            identityEditorModel = null;

            if (editor.ShouldOpenAppearanceEditorAfterClose)
            {
                if (identity.IsEmpty)
                {
                    _player.SendServerMessage("Tożsamość jest nieprawidłowa.", ColorConstants.Red);
                    ForceClose();
                    return;
                }

                if (identityState.IsPolymorphed)
                {
                    _player.SendServerMessage("Nie można edytować wyglądu tożsamości będąc pod wpływem polimorfii.", ColorConstants.Red);
                    ForceClose();
                    return;
                }

                identitySelectorModel ??= CreateSelectorModel();

                var info = identitySelectorModel.SelectedIdentity;

                ForceClose();

                identityState.SetFalseIdentity(info);

                CharacterAppearanceService.OpenAppearanceEditorNUI(_player, EditorFlags.Head | EditorFlags.Tattoo | EditorFlags.HairColor | EditorFlags.FreeOfCharge);
            }
            else OpenIdentitySelector();
        }



        void OpenPortraitPicker()
        {
            if (state == State.Create) portraitPickerController = new(_player, IdentityManager.PortraitStorage,_player.LoginCreature!.PortraitResRef+'l', identityEditorModel!.Gender);

            else if (state == State.Edit) portraitPickerController = new(_player, IdentityManager.PortraitStorage, identitySelectorModel!.SelectedIdentity.Identity.Portrait+'l', identityEditorModel!.Gender);

            else
            {
                _log.Error("Invalid state " + state.ToString());
                ForceClose();
                return;
            }

            identityEditorController!.SetInputEnabled(false);

            state = State.PickPortrait;

            portraitPickerController.ClosedEvent += OnPortraitPickerClosed;
        }


        void OnPortraitPickerClosed(ControllerBase controller, object? data)
        {
            portraitPickerController!.ClosedEvent -= OnPortraitPickerClosed;
            portraitPickerController = null;

            var str = (string?)data;

            if (controller is not UI.Controller.PortraitPicker)
            {
                _log.Error($"Handler called with controller which is not {nameof(UI.Controller.PortraitPicker)}", nameof(controller));
                ForceClose();
            }
            else if (state == State.PickPortrait)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    identityEditorModel!.Portrait = str + 'l';
                }
                
                identityEditorController!.SetInputEnabled(true);
                state = identityEditorModel!.IsCreatingNew ? State.Create : State.Edit;
            }
            else
            {
                _log.Error("Invalid state " + state.ToString());
                ForceClose();
            }
        }


    }
}