using Anvil.API;
using NuiMVC;

using SelectorView = CharacterIdentity.UI.View.IdentitySelector;
using SelectorModel = CharacterIdentity.UI.Model.IdentitySelector;
using System.Linq;
using System;

namespace CharacterIdentity.UI.Controller
{
    internal sealed class IdentitySelector : ControllerBase
    {
        private SelectorModel model;

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public IdentitySelector(NwPlayer player, SelectorModel model) : base(player, SelectorView.NuiWindow)
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            SetModel(model);

            SetWatch(SelectorView.ComboSelectionProperty, true);
        }


        public void SetModel(SelectorModel model)
        {
            this.model = model;

            var entries = model.Identities.Select((info, idx) => new NuiComboEntry(info.Name, idx)).ToList();
            if (entries.Count > CharacterIdentityService.ServiceConfig.MaximumFalseIdentities)
                entries = entries[..CharacterIdentityService.ServiceConfig.MaximumFalseIdentities];

            SetValue(SelectorView.ComboEntriesProperty, entries);
            SetValue(SelectorView.ComboSelectionProperty, this.model.SelectedIndex);

            SetValue(SelectorView.PortraitResRefProperty, this.model.HasFalseIdentities ? this.model.SelectedIdentity.Identity.Portrait + 'l' : this.model.TrueIdentity.Identity.Portrait + 'l');
            SetValue(SelectorView.PortraitEnabledProperty, this.model.HasFalseIdentities);
            SetValue(SelectorView.ComboEnabledProperty, this.model.HasFalseIdentities);

            SetValue(SelectorView.NewBtnEnabledProperty, this.model.CanCreate);

            if (this.model.CanCreate)
            {
                if (this.model.HowManyIdentitiesCanBeCreated == 1) SetValue(SelectorView.NewBtnTooltipProperty, "Możesz stworzyć jeszcze jedną fałszywą tożsamosć");

                else if (this.model.HowManyIdentitiesCanBeCreated <= 4) SetValue(SelectorView.NewBtnTooltipProperty, $"Możesz stworzyć jeszcze {this.model.HowManyIdentitiesCanBeCreated} fałszywe tożsamości.");

                else SetValue(SelectorView.NewBtnTooltipProperty, $"Możesz stworzyć jeszcze {this.model.HowManyIdentitiesCanBeCreated} fałszywych tożsamości");
            }
            else
            {
                SetValue(SelectorView.NewBtnDisabledTooltipProperty, this.model.WhyCreationIsDisabled);
            }

            if (this.model.HasFalseIdentities)
            {
                SetValue(SelectorView.EditBtnDisabledTooltipProperty, "Nie można edytować aktywnej tożsamości");
                SetValue(SelectorView.DeleteBtnDisabledTooltipProperty, "Nie można usunąć aktywnej tożsamości");
            }
            else
            {
                SetValue(SelectorView.EditBtnDisabledTooltipProperty, "");
                SetValue(SelectorView.DeleteBtnDisabledTooltipProperty, "");
            }

            SetValue(SelectorView.EditBtnsEnabledProperty, this.model.CanEdit);

            SetValue(SelectorView.RestoreBtnEnabledProperty, this.model.CanRestore);
            SetValue(SelectorView.PickBtnEnabledProperty, this.model.CanPick);

            if (!this.model.CanPick)
            {
                SetValue(SelectorView.PickButtonDisabledTooltipProperty, this.model.HasFalseIdentities
                ? "To twoja aktywna tożsamość"
                : "Nie posiadasz fałszywych tożsamości");
            }
        }


        public event Action<bool>? EditorOpen;
        public event System.Action? IdentityChanged;
        public event Action<int>? IdentityDeleted;


        protected override void OnClick(string elementId)
        {
            switch (elementId)
            {
                case nameof(SelectorView.NewButton):
                    EditorOpen?.Invoke(true);
                    break;

                case nameof(SelectorView.EditButton):
                    EditorOpen?.Invoke(false);
                    break;

                case nameof(SelectorView.DeleteButton):
                    int id = model.SelectedIdentity.ID;
                    IdentityDeleted?.Invoke(id);
                    break;

                case nameof(SelectorView.PickButton):
                    model.PickIdentity();
                    IdentityChanged?.Invoke();
                    SetModel(model);
                    break;

                case nameof(SelectorView.RestoreButton):
                    model.RestoreOriginal();
                    IdentityChanged?.Invoke();
                    SetModel(model);
                    break;
            }
        }

        protected override object? OnClose()
        {
            SetWatch(SelectorView.ComboSelectionProperty, false);
            return null;
        }

        protected override void Update(string elementId)
        {
            int index = GetValue(SelectorView.ComboSelectionProperty);

            model.SelectFalseIdentity(index);

            SetValue(SelectorView.PortraitResRefProperty, model.Identities[index].Identity.Portrait+'l');

            SetValue(SelectorView.EditBtnsEnabledProperty, model.CanEdit);

            SetValue(SelectorView.RestoreBtnEnabledProperty, model.CanRestore);
            SetValue(SelectorView.PickBtnEnabledProperty, model.CanPick);

            if (!model.CanCreate)
                SetValue(SelectorView.NewBtnDisabledTooltipProperty, model.WhyCreationIsDisabled);

            if (model.HasFalseIdentities)
            {
                SetValue(SelectorView.EditBtnDisabledTooltipProperty, "Nie można edytować aktywnej tożsamości");
                SetValue(SelectorView.DeleteBtnDisabledTooltipProperty, "Nie można usunąć aktywnej tożsamości");
            }
            else
            {
                SetValue(SelectorView.EditBtnDisabledTooltipProperty, "");
                SetValue(SelectorView.DeleteBtnDisabledTooltipProperty, "");
            }
        }
    }
}