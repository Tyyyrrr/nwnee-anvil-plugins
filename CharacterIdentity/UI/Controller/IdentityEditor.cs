using System;
using System.Collections.Frozen;
using System.Collections.Generic;

using Anvil.API;

using NuiMVC;

using EditorView = CharacterIdentity.UI.View.IdentityEditor;
using EditorModel = CharacterIdentity.UI.Model.IdentityEditor;



namespace CharacterIdentity.UI.Controller
{
    /// <summary>
    ///  TODO: implement appearance edit
    /// </summary>
    internal sealed class IdentityEditor : ControllerBase
    {
        private readonly EditorModel _model;

        internal event System.Action? PortraitOpen;

        public IdentityEditor(NwPlayer player, EditorModel model) : base(player, EditorView.NuiWindow)
        {
            ShouldOpenAppearanceEditorAfterClose = false;

            _model = model;

            bool isDataValid = _model.IsDataValid;


            SetValue(EditorView.PortraitProperty, _model.Portrait);

            SetValue(EditorView.FirstNameProperty, _model.FirstName);
            SetValue(EditorView.FirstNameEncouragedProperty, !isDataValid);

            SetValue(EditorView.LastNameProperty, _model.LastName);

            SetValue(EditorView.NameCharactersCountProperty, _model.FullNameCharacters.ToString() + "/" + CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters.ToString());


            SetValue(EditorView.MinAgeProperty, _model.MinimumAge);
            SetValue(EditorView.MaxAgeProperty, _model.MaximumAge);

            SetValue(EditorView.AgeProperty, _model.Age);

            SetValue(EditorView.AgeLabelProperty, "Wiek: " + _model.Age.ToString());


            SetValue(EditorView.DescriptionProperty, _model.Description);

            SetValue(EditorView.DescCharactersCountLabelProperty, _model.Description.Length.ToString() + "/" + CharacterIdentityService.IdentityEditorConfig.MaximumDescriptionCharacters.ToString());
            SetValue(EditorView.DescCharactersCountColorProperty, ColorConstants.White);


            SetValue(EditorView.ApplyBtnEnabledProperty, isDataValid);

            SetValue(EditorView.LowerAgeBtnEnabledProperty, isDataValid && _model.Age != _model.MinimumAge);
            SetValue(EditorView.RaiseAgeBtnEnabledProperty, isDataValid && _model.Age != _model.MaximumAge);

            SetValue(EditorView.ApplyBtnEncouragedProperty, _model.IsEverythingProvided);

            SetValue(EditorView.WindowAcceptsInputProperty, true);

            bool isMale = _model.Gender == Gender.Male;
            SetValue(EditorView.MaleCheckboxSelectedProperty, isMale);
            SetValue(EditorView.FemaleCheckboxSelectedProperty, !isMale);
            if(_model.CanChangeGender)
            {
                SetValue(EditorView.MaleCheckboxEnabledProperty, !isMale);
                SetValue(EditorView.FemaleCheckboxEnabledProperty, isMale);
                SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, string.Empty);
            }
            else
            {
                SetValue(EditorView.MaleCheckboxEnabledProperty, false);
                SetValue(EditorView.FemaleCheckboxEnabledProperty, false);
                if(!_model.SubraceCanChangeGender)
                    SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, $"Twoja podrasa nie pozwala na zmianę płci dla fałszywej tożsamośći.");
                else SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, $"Brakujące punkty blefu: {_model.BluffRemainingForGenderChange}");
            }
            SetWatch(EditorView.MaleCheckboxSelectedProperty, true);
            SetWatch(EditorView.FemaleCheckboxSelectedProperty, true);

            string windowTitle = _model.IsCreatingNew
                ? ("Nowa fałszywa tożsamość postaci " + player.ControlledCreature!.OriginalFirstName)
                : $"Edycja tożsamości {_model.FirstName}";

            SetValue(EditorView.WindowTitleProperty, windowTitle);

            SetWatch(EditorView.FirstNameProperty, true);
            SetWatch(EditorView.LastNameProperty, true);
            SetWatch(EditorView.AgeProperty, true);
            SetWatch(EditorView.DescriptionProperty, true);
        }

        public void SetInputEnabled(bool enabled)
        {
            SetValue(EditorView.WindowAcceptsInputProperty, enabled);

            if (enabled) 
                SetValue(EditorView.PortraitProperty, _model.Portrait);
        }

        private static readonly FrozenDictionary<string, Action<IdentityEditor>> _clickCallbacks = new Dictionary<string, Action<IdentityEditor>>()
        {
            {nameof(EditorView.ApplyButton), e => e.Close() },

            {nameof(EditorView.AbortButton),
                e =>
                {
                    e._model.FirstName = string.Empty; // invalidates the result making it Identity.Empty
                    e.Close();
                }
            },

            {nameof(EditorView.LowerAgeButton),
                e =>
                {
                    e._model.Age--;
                    e.SetValue(EditorView.AgeProperty, e._model.Age);
                }
            },

            {nameof(EditorView.RaiseAgeButton),
                e =>
                {
                    e._model.Age++;
                    e.SetValue(EditorView.AgeProperty, e._model.Age);
                }
            },

            {nameof(EditorView.AppearanceButton), e => {e.ShouldOpenAppearanceEditorAfterClose = true; e.Close();}}

        }.ToFrozenDictionary();

        public bool ShouldOpenAppearanceEditorAfterClose {get;private set;}

        private static readonly FrozenDictionary<string, Action<IdentityEditor>> _propertyUpdates = new Dictionary<string, Action<IdentityEditor>>()
        {
            {nameof(EditorView.AgeProperty),
                e =>
                {
                    var val = e.GetValue(EditorView.AgeProperty);

                    e._model.Age = val;

                    if(e._model.Age != val) 
                        e.SetValue(EditorView.AgeProperty, e._model.Age);
                    else
                        e.SetValue(EditorView.AgeLabelProperty, "Wiek: " + e._model.Age.ToString());
                }
            },

            {nameof(EditorView.FirstNameProperty),
                e =>
                {
                    var val = e.GetValue(EditorView.FirstNameProperty);

                    if(e._model.FirstName == val) return;

                    e._model.FirstName = val;

                    if(e._model.FirstName != val)
                    {
                        e.SetValue(EditorView.FirstNameProperty, e._model.FirstName);
                    }
                    else
                    {
                        e.SetValue(EditorView.NameCharactersCountProperty, e._model.FullNameCharacters.ToString() + "/" + CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters.ToString());
                    }
                }
            },

            {nameof(EditorView.LastNameProperty),
                e =>
                {
                    var val = e.GetValue(EditorView.LastNameProperty);

                    if(e._model.LastName == val) return;

                    e._model.LastName = val;

                    if(e._model.LastName != val)
                        e.SetValue(EditorView.LastNameProperty, e._model.LastName);
                    else{
                        e.SetValue(EditorView.NameCharactersCountProperty, e._model.FullNameCharacters.ToString() + "/" + CharacterIdentityService.IdentityEditorConfig.MaximumNameCharacters.ToString());
                    }
                }
            },

            {nameof(EditorView.DescriptionProperty),
                e =>
                {
                    var val = e.GetValue(EditorView.DescriptionProperty);

                    if(e._model.Description == val) return;

                    e._model.Description = val;

                    if(e._model.Description != val)
                        e.SetValue(EditorView.DescriptionProperty, val);
                    else
                    {
                        e.SetValue(EditorView.DescCharactersCountLabelProperty, e._model.Description.Length.ToString() + '/' +CharacterIdentityService.IdentityEditorConfig.MaximumDescriptionCharacters.ToString()); // todo: use config value
                        e.SetValue(EditorView.DescCharactersCountColorProperty, e._model.Description.Length >= CharacterIdentityService.IdentityEditorConfig.MaximumDescriptionCharacters ? ColorConstants.Red : ColorConstants.White);
                    }
                }
            },

            {nameof(EditorView.MaleCheckboxSelectedProperty),
                e =>
                {
                    if(e.GetValue(EditorView.MaleCheckboxSelectedProperty))
                    {
                        e._model.Gender = Gender.Male;
                        e.SetValue(EditorView.MaleCheckboxEnabledProperty, false);
                        e.SetValue(EditorView.FemaleCheckboxSelectedProperty, false);
                        e.SetValue(EditorView.FemaleCheckboxEnabledProperty, true);
                    }
                }
            },

            {nameof(EditorView.FemaleCheckboxSelectedProperty),
                e =>
                {
                    if(e.GetValue(EditorView.FemaleCheckboxSelectedProperty))
                    {
                        e._model.Gender = Gender.Female;
                        e.SetValue(EditorView.FemaleCheckboxEnabledProperty, false);
                        e.SetValue(EditorView.MaleCheckboxSelectedProperty, false);
                        e.SetValue(EditorView.MaleCheckboxEnabledProperty, true);
                    }
                }
            }

        }.ToFrozenDictionary();

        protected override void OnMouseDown(string elementId)
        {
            if (elementId == EditorView.PortraitImage.Id)
                PortraitOpen?.Invoke();
        }
        protected override void OnClick(string elementId)
        {
            if (_clickCallbacks.TryGetValue(elementId, out var callback))
                callback.Invoke(this);
        }

        protected override void Update(string elementId)
        {
            if (_propertyUpdates.TryGetValue(elementId, out var callback))
                callback.Invoke(this);

            UpdateOverallState();
        }

        private void UpdateOverallState()
        {
            bool isDataValid = _model.IsDataValid;

            SetValue(EditorView.ApplyBtnEnabledProperty, isDataValid);

            SetValue(EditorView.LowerAgeBtnEnabledProperty, isDataValid && _model.Age != _model.MinimumAge);
            SetValue(EditorView.RaiseAgeBtnEnabledProperty, isDataValid && _model.Age != _model.MaximumAge);
                        
            SetValue(EditorView.ApplyBtnEncouragedProperty, _model.IsEverythingProvided);
            SetValue(EditorView.FirstNameEncouragedProperty, !isDataValid);

            bool isMale = _model.Gender == Gender.Male;
            SetValue(EditorView.MaleCheckboxSelectedProperty, isMale);
            SetValue(EditorView.FemaleCheckboxSelectedProperty, !isMale);
            if(_model.CanChangeGender)
            {
                SetValue(EditorView.MaleCheckboxEnabledProperty, !isMale);
                SetValue(EditorView.FemaleCheckboxEnabledProperty, isMale);
                SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, string.Empty);
            }
            else
            {
                SetValue(EditorView.MaleCheckboxEnabledProperty, false);
                SetValue(EditorView.FemaleCheckboxEnabledProperty, false);
                if(!_model.SubraceCanChangeGender)
                    SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, $"Twoja podrasa nie pozwala na zmianę płci dla fałszywej tożsamośći.");
                else SetValue(EditorView.GenderCheckboxDisabledTooltipProperty, $"Brakujące punkty blefu: {_model.BluffRemainingForGenderChange}");
            }
        }

        protected override object? OnClose() => _model.GetIdentity();

    }
}