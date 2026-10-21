using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard
{
    /// <summary>
    /// Utility class to help facilitate input field relationship with <see cref="XRKeyboard"/>.
    /// This version supports several input fields on one display. Whichever field is selected
    /// or touched receives the typing, and the other fields are left untouched.
    /// </summary>
    public class MultiFieldKeyboardDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("If true, every TMP_InputField under this object is picked up automatically (including inactive ones), so the keyboard opens for any field you touch. No need to assign them by hand.")]
        bool m_AutoFindFields = true;

        /// <summary>
        /// If true, every input field under this object is found automatically.
        /// </summary>
        public bool autoFindFields
        {
            get => m_AutoFindFields;
            set => m_AutoFindFields = value;
        }

        [SerializeField, Tooltip("Extra input fields linked to this display (optional when Auto Find Fields is on). The one you select receives the typing.")]
        TMP_InputField[] m_InputFields;

        // Old single-field slot. Hidden, but kept so scenes that used the previous
        // version of this script do not lose their existing reference.
        [SerializeField, HideInInspector]
        TMP_InputField m_InputField;

        /// <summary>
        /// Input fields linked to this display.
        /// </summary>
        public TMP_InputField[] inputFields
        {
            get => m_InputFields;
            set
            {
                UnregisterFields();
                m_InputFields = value;
                BuildFieldList();
                RegisterFields();
            }
        }

        /// <summary>
        /// The field that is currently receiving the keyboard text (null until one is selected).
        /// </summary>
        public TMP_InputField currentInputField => m_CurrentField;

        [SerializeField, Tooltip("Keyboard for this display to monitor and interact with. If empty this will default to the GlobalNonNativeKeyboard keyboard.")]
        XRKeyboard m_Keyboard;

        /// <summary>
        /// Keyboard for this display to monitor and interact with. If empty this will default to the <see cref="GlobalNonNativeKeyboard"/> keyboard.
        /// </summary>
        public XRKeyboard keyboard
        {
            get => m_Keyboard;
            set => SetKeyboard(value);
        }

        [SerializeField, Tooltip("If true, this display will use the keyboard reference. If false or if the keyboard field is empty, this display will use global keyboard.")]
        bool m_UseSceneKeyboard;

        /// <summary>
        /// If true, this display will use the keyboard reference. If false or if the keyboard field is empty,
        /// this display will use global keyboard.
        /// </summary>
        public bool useSceneKeyboard
        {
            get => m_UseSceneKeyboard;
            set => m_UseSceneKeyboard = value;
        }

        [SerializeField, Tooltip("If true, this display will update with each key press. If false, this display will update on OnTextSubmit.")]
        bool m_UpdateOnKeyPress = true;

        /// <summary>
        /// If true, this display will update with each key press. If false, this display will update on OnTextSubmit.
        /// </summary>
        public bool updateOnKeyPress
        {
            get => m_UpdateOnKeyPress;
            set => m_UpdateOnKeyPress = value;
        }

        [SerializeField, Tooltip("If true, this display will always subscribe to the keyboard updates. If false, this display will subscribe to keyboard when an input field gains focus.")]
        bool m_AlwaysObserveKeyboard;

        /// <summary>
        /// If true, this display will always subscribe to the keyboard updates. If false, this display will subscribe
        /// to keyboard when an input field gains focus.
        /// </summary>
        public bool alwaysObserveKeyboard
        {
            get => m_AlwaysObserveKeyboard;
            set => m_AlwaysObserveKeyboard = value;
        }

        [SerializeField, Tooltip("If true, this display will use the selected input field's character limit to limit the update text from the keyboard and will pass this into the keyboard when opening.")]
        public bool m_MonitorInputFieldCharacterLimit;

        /// <summary>
        /// If true, this display will use the selected input field's character limit to limit the update text from the keyboard
        /// and will pass this into the keyboard when opening.
        /// </summary>
        public bool monitorInputFieldCharacterLimit
        {
            get => m_MonitorInputFieldCharacterLimit;
            set => m_MonitorInputFieldCharacterLimit = value;
        }

        [SerializeField, Tooltip("If true, this display will clear the selected input field text on text submit from the keyboard.")]
        public bool m_ClearTextOnSubmit;

        /// <summary>
        /// If true, this display will clear the selected input field text on text submit from the keyboard.
        /// </summary>
        public bool clearTextOnSubmit
        {
            get => m_ClearTextOnSubmit;
            set => m_ClearTextOnSubmit = value;
        }

        [SerializeField, Tooltip("If true, this display will clear the input field text when the keyboard opens for it. Leave this off for login and sign-up forms.")]
        public bool m_ClearTextOnOpen;

        /// <summary>
        /// If true, this display will clear the input field text when the keyboard opens for it.
        /// </summary>
        public bool clearTextOnOpen
        {
            get => m_ClearTextOnOpen;
            set => m_ClearTextOnOpen = value;
        }

        [SerializeField, Tooltip("If true, this display will close the keyboard it is observing when this GameObject is disabled.")]
        public bool m_HideKeyboardOnDisable = true;

        /// <summary>
        /// If true, this display will close the keyboard it is observing when this GameObject is disabled.
        /// </summary>
        /// <remarks>If this display is not observing a keyboard when disabled, this will have not effect on open keyboards.</remarks>
        public bool hideKeyboardOnDisable
        {
            get => m_HideKeyboardOnDisable;
            set => m_HideKeyboardOnDisable = value;
        }

        [SerializeField, Tooltip("The event that is called when this display receives a text submitted event from the keyboard. Invoked with the keyboard text as a parameter.")]
        UnityEvent<string> m_OnTextSubmitted = new UnityEvent<string>();

        /// <summary>
        /// The event that is called when this display receives a text submitted event from the keyboard.
        /// </summary>
        public UnityEvent<string> onTextSubmitted
        {
            get => m_OnTextSubmitted;
            set => m_OnTextSubmitted = value;
        }

        [SerializeField, Tooltip("The event that is called when this display opens a keyboard.")]
        UnityEvent m_OnKeyboardOpened = new UnityEvent();

        /// <summary>
        /// The event that is called when this display opens a keyboard.
        /// </summary>
        public UnityEvent onKeyboardOpened
        {
            get => m_OnKeyboardOpened;
            set => m_OnKeyboardOpened = value;
        }

        [SerializeField, Tooltip("The event that is called when the keyboard this display is observing is closed.")]
        UnityEvent m_OnKeyboardClosed = new UnityEvent();

        /// <summary>
        /// The event that is called when the keyboard this display is observing is closed.
        /// </summary>
        public UnityEvent onKeyboardClosed
        {
            get => m_OnKeyboardClosed;
            set => m_OnKeyboardClosed = value;
        }

        [SerializeField, Tooltip("The event that is called when the keyboard changes focus and none of this display's fields are focused.")]
        UnityEvent m_OnKeyboardFocusChanged = new UnityEvent();

        /// <summary>
        /// The event that is called when the keyboard changes focus and none of this display's fields are focused.
        /// </summary>
        public UnityEvent onKeyboardFocusChanged
        {
            get => m_OnKeyboardFocusChanged;
            set => m_OnKeyboardFocusChanged = value;
        }

        // Active keyboard for this display
        XRKeyboard m_ActiveKeyboard;

        bool m_IsActivelyObservingKeyboard;

        // Working list of fields (auto-found + array + legacy single field, no nulls, no duplicates)
        readonly List<TMP_InputField> m_Fields = new List<TMP_InputField>();

        // One select listener per field, stored so each can be removed later
        readonly Dictionary<TMP_InputField, UnityAction<string>> m_SelectHandlers =
            new Dictionary<TMP_InputField, UnityAction<string>>();

        // The field that is receiving the keyboard text right now
        TMP_InputField m_CurrentField;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Awake()
        {
            // Set active keyboard to any serialized keyboard
            m_ActiveKeyboard = m_Keyboard;

            BuildFieldList();

            foreach (TMP_InputField field in m_Fields)
            {
                // resetOnDeActivation should be false so the caret position does not break with the keyboard interaction
                field.resetOnDeActivation = false;

                // shouldHideSoftKeyboard should be true so there is no conflict with the spatial keyboard and the system keyboard
                field.shouldHideSoftKeyboard = true;
            }

            if (m_AlwaysObserveKeyboard && m_ActiveKeyboard != null)
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnEnable()
        {
            BuildFieldList();
            RegisterFields();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDisable()
        {
            UnregisterFields();

            // Close the keyboard this display is observing
            var isObservingKeyboard = m_ActiveKeyboard != null && m_ActiveKeyboard.gameObject.activeInHierarchy && m_IsActivelyObservingKeyboard;
            if (m_HideKeyboardOnDisable && isObservingKeyboard && m_ActiveKeyboard.isOpen)
                m_ActiveKeyboard.Close();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void OnDestroy()
        {
            StopObservingKeyboard(m_ActiveKeyboard);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Start()
        {
            // Set active keyboard to global keyboard if needed
            if (m_ActiveKeyboard == null || !m_UseSceneKeyboard)
                m_ActiveKeyboard = GlobalNonNativeKeyboard.instance.keyboard;

            // Observe keyboard if always observe is true
            var observeOnStart = m_AlwaysObserveKeyboard && m_ActiveKeyboard != null && !m_IsActivelyObservingKeyboard;
            if (observeOnStart)
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        // ---------- Field handling ----------

        void BuildFieldList()
        {
            m_Fields.Clear();

            // Every input field under this object, including inactive ones
            if (m_AutoFindFields)
            {
                foreach (TMP_InputField field in GetComponentsInChildren<TMP_InputField>(true))
                {
                    if (field != null && !m_Fields.Contains(field))
                        m_Fields.Add(field);
                }
            }

            if (m_InputFields != null)
            {
                foreach (TMP_InputField field in m_InputFields)
                {
                    if (field != null && !m_Fields.Contains(field))
                        m_Fields.Add(field);
                }
            }

            // Carry over a field assigned with the old single-field version of this script
            if (m_InputField != null && !m_Fields.Contains(m_InputField))
                m_Fields.Add(m_InputField);
        }

        void RegisterFields()
        {
            foreach (TMP_InputField field in m_Fields)
            {
                if (m_SelectHandlers.ContainsKey(field))
                    continue;

                field.resetOnDeActivation = false;
                field.shouldHideSoftKeyboard = true;

                // Each field gets its own listener that remembers which field it belongs to
                TMP_InputField captured = field;

                // Opens the keyboard when the field becomes selected
                UnityAction<string> handler = _ => OnInputFieldGainedFocus(captured);
                m_SelectHandlers[field] = handler;
                field.onSelect.AddListener(handler);

                // Opens the keyboard when the field is touched again after the keyboard was closed
                var relay = field.GetComponent<InputFieldTouchRelay>();
                if (relay == null)
                    relay = field.gameObject.AddComponent<InputFieldTouchRelay>();
                relay.onTouched = () => OnFieldTouched(captured);
            }
        }

        void UnregisterFields()
        {
            foreach (KeyValuePair<TMP_InputField, UnityAction<string>> pair in m_SelectHandlers)
            {
                if (pair.Key == null)
                    continue;

                pair.Key.onSelect.RemoveListener(pair.Value);

                var relay = pair.Key.GetComponent<InputFieldTouchRelay>();
                if (relay != null)
                    relay.onTouched = null;
            }

            m_SelectHandlers.Clear();
        }

        void OnFieldTouched(TMP_InputField field)
        {
            if (field == null)
                return;

            // Keyboard is already open for this field, nothing to do
            if (m_ActiveKeyboard != null && m_ActiveKeyboard.isOpen && m_CurrentField == field)
                return;

            OnInputFieldGainedFocus(field);
        }

        bool IsAnyFieldFocused()
        {
            foreach (TMP_InputField field in m_Fields)
            {
                if (field != null && field.isFocused)
                    return true;
            }

            return false;
        }

        // ---------- Keyboard handling ----------

        void SetKeyboard(XRKeyboard updateKeyboard, bool observeKeyboard = true)
        {
            if (ReferenceEquals(updateKeyboard, m_Keyboard))
                return;

            StopObservingKeyboard(m_ActiveKeyboard);

            // Update serialized referenced
            m_Keyboard = updateKeyboard;

            // Update private keyboard
            m_ActiveKeyboard = m_Keyboard;

            if (m_ActiveKeyboard != null && (observeKeyboard || m_AlwaysObserveKeyboard))
                StartObservingKeyboard(m_ActiveKeyboard);
        }

        void StartObservingKeyboard(XRKeyboard activeKeyboard)
        {
            if (activeKeyboard == null || m_IsActivelyObservingKeyboard)
                return;

            activeKeyboard.onTextUpdated.AddListener(OnTextUpdate);
            activeKeyboard.onTextSubmitted.AddListener(OnTextSubmit);
            activeKeyboard.onClosed.AddListener(KeyboardClosing);
            activeKeyboard.onOpened.AddListener(KeyboardOpening);
            activeKeyboard.onFocusChanged.AddListener(KeyboardFocusChanged);

            m_IsActivelyObservingKeyboard = true;
        }

        void StopObservingKeyboard(XRKeyboard activeKeyboard)
        {
            if (activeKeyboard == null)
                return;

            activeKeyboard.onTextUpdated.RemoveListener(OnTextUpdate);
            activeKeyboard.onTextSubmitted.RemoveListener(OnTextSubmit);
            activeKeyboard.onClosed.RemoveListener(KeyboardClosing);
            activeKeyboard.onOpened.RemoveListener(KeyboardOpening);
            activeKeyboard.onFocusChanged.RemoveListener(KeyboardFocusChanged);

            m_IsActivelyObservingKeyboard = false;
        }

        void OnInputFieldGainedFocus(TMP_InputField field)
        {
            if (field == null || m_ActiveKeyboard == null)
                return;

            bool switchingField = m_CurrentField != field;

            // From now on, keyboard text goes to this field only
            m_CurrentField = field;

            // Same field selected again while already observing: sync, attempt to reposition, and early out.
            // Displays that are always observing keyboards call open to ensure they sync with the keyboard.
            if (m_IsActivelyObservingKeyboard && !alwaysObserveKeyboard && !switchingField)
            {
                if (!m_UseSceneKeyboard || m_Keyboard == null)
                    GlobalNonNativeKeyboard.instance.RepositionKeyboardIfOutOfView();

                // Sync input field caret position with keyboard caret position
                if (field.stringPosition != m_ActiveKeyboard.caretPosition)
                    field.stringPosition = m_ActiveKeyboard.caretPosition;

                return;
            }

            if (m_ClearTextOnOpen)
                field.text = string.Empty;

            // If not using a scene keyboard, use global keyboard.
            if (!m_UseSceneKeyboard || m_Keyboard == null)
            {
                GlobalNonNativeKeyboard.instance.ShowKeyboard(field, m_MonitorInputFieldCharacterLimit);
            }
            else
            {
                m_ActiveKeyboard.Open(field, m_MonitorInputFieldCharacterLimit);
            }

            // Sync input field caret position with keyboard caret position
            if (field.stringPosition != m_ActiveKeyboard.caretPosition)
                field.stringPosition = m_ActiveKeyboard.caretPosition;

            // This display is opening the keyboard
            m_OnKeyboardOpened.Invoke();

            StartObservingKeyboard(m_ActiveKeyboard);
        }

        void OnTextSubmit(KeyboardTextEventArgs args)
        {
            UpdateText(args.keyboardText);
            m_OnTextSubmitted?.Invoke(args.keyboardText);

            if (m_ClearTextOnSubmit && m_CurrentField != null)
                m_CurrentField.text = string.Empty;
        }

        void OnTextUpdate(KeyboardTextEventArgs args)
        {
            if (!m_UpdateOnKeyPress)
                return;

            UpdateText(args.keyboardText);
        }

        void UpdateText(string text)
        {
            // Only the selected field is updated
            if (m_CurrentField == null)
                return;

            var updatedText = text;

            // Clip updated text to substring (a limit of 0 means no limit)
            if (m_MonitorInputFieldCharacterLimit
                && m_CurrentField.characterLimit > 0
                && updatedText.Length >= m_CurrentField.characterLimit)
            {
                updatedText = updatedText.Substring(0, m_CurrentField.characterLimit);
            }

            m_CurrentField.text = updatedText;

            // Update input field caret position with keyboard caret position
            if (m_CurrentField.stringPosition != m_ActiveKeyboard.caretPosition)
                m_CurrentField.stringPosition = m_ActiveKeyboard.caretPosition;
        }

        void KeyboardOpening(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!IsAnyFieldFocused() && !m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);
        }

        void KeyboardClosing(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);

            m_OnKeyboardClosed.Invoke();
        }

        void KeyboardFocusChanged(KeyboardTextEventArgs args)
        {
            Debug.Assert(args.keyboard == m_ActiveKeyboard);

            if (args.keyboard != m_ActiveKeyboard)
                return;

            if (!IsAnyFieldFocused() && !m_AlwaysObserveKeyboard)
                StopObservingKeyboard(m_ActiveKeyboard);

            // The keyboard changed focus and none of this display's fields are in focus
            if (!IsAnyFieldFocused())
                m_OnKeyboardFocusChanged.Invoke();
        }
    }

    /// <summary>
    /// Added automatically to each input field by <see cref="MultiFieldKeyboardDisplay"/>.
    /// Reports touches/clicks, even when the field is already selected and onSelect would not fire again.
    /// </summary>
    public class InputFieldTouchRelay : MonoBehaviour, IPointerClickHandler
    {
        public System.Action onTouched;

        public void OnPointerClick(PointerEventData eventData)
        {
            onTouched?.Invoke();
        }
    }
}