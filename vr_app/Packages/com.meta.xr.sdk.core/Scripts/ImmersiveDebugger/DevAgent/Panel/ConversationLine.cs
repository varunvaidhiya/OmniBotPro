/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine;
using System.Collections;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// This is a <see cref="MonoBehaviour"/> for the conversation line UI element on LLM dialog panel of Immersive Debugger.
    /// Contains UI elements like Background, Message label, and type indicator.
    /// Display the content of the <see cref="ConversationEntry"/> and make sure layout is correct with clamping.
    /// For more info about Immersive Debugger, check out the [official doc](https://developer.oculus.com/documentation/unity/immersivedebugger-overview)
    /// </summary>
    internal class ConversationLine : UserInterface.Generic.InteractableController
    {
        private Label _label;
        private Flex _flex;
        private Background _typeIndicator;

        private ConversationEntry _entry;
        private Coroutine _blinkingCursorCoroutine;
        private const float CURSOR_BLINK_INTERVAL = 0.5f; // Blink every 0.5 seconds

        internal ConversationEntry Entry
        {
            get => _entry;
            set
            {
                _entry = value;
                if (_entry != null)
                {
                    UpdateDisplay();
                }
            }
        }

        protected override void Setup(Controller owner)
        {
            base.Setup(owner);

            // Flex - use instantiated style so it can adapt height
            _flex = Append<Flex>("line");
            _flex.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLineFlex");

            // Initially hide the line since there's no entry assigned
            gameObject.SetActive(false);
        }

        private void SetupUIElements()
        {
            // Clear existing UI elements if any
            if (_label != null || _typeIndicator != null)
            {
                _flex.Clear(true);
                _label = null;
                _typeIndicator = null;
            }

            // Determine if this is a user message (should be right-aligned)
            var isUserMessage = _entry?.Type == ConversationEntry.MessageType.User;

            if (!isUserMessage)
            {
                // For non-user messages: left-aligned text with dot on the left
                _flex.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLineFlex");

                // Type indicator first (left side)
                _typeIndicator = _flex.Append<Background>("typeIndicator");
                _typeIndicator.LayoutStyle = Style.Load<LayoutStyle>("ConversationDot");

                // Message Label second (right side)
                _label = _flex.Append<Label>("message");
                _label.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLineLabel");
                _label.TextStyle = Style.Load<TextStyle>("ConversationLineLabel");
            }
            else
            {
                // For user messages: right-aligned text with dot on the right
                _flex.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLineFlexRightAligned");

                // Type indicator second (right side)
                _typeIndicator = _flex.Append<Background>("typeIndicator");
                _typeIndicator.LayoutStyle = Style.Load<LayoutStyle>("ConversationDotRightAligned");

                // Message Label first (left side, but right-aligned text)
                _label = _flex.Append<Label>("message");
                _label.LayoutStyle = Style.Instantiate<LayoutStyle>("ConversationLineLabelRightAligned");
                _label.TextStyle = Style.Load<TextStyle>("ConversationLineLabelRightAligned");
            }

            // Configure text wrapping for multiline support
            _label.Text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.Text.verticalOverflow = VerticalWrapMode.Overflow;

            // Verify UI element creation
            if (_typeIndicator == null)
            {
                // Failed to create type indicator
            }
            if (_label == null)
            {
                // Failed to create label
            }
        }

        public void UpdateDisplay()
        {
            if (_entry == null)
            {
                // Hide the line when there's no entry to prevent empty lines
                gameObject.SetActive(false);
                return;
            }

            // Show the line when there's an entry
            gameObject.SetActive(true);

            // Setup UI elements based on message type
            SetupUIElements();

            UpdateRefreshLayout(true);
            Owner.UpdateRefreshLayout(true);

            // Set message text with truncation indicator
            var displayText = _entry.Message;
            if (_entry.HasTruncatedContent)
            {
                displayText += "...";
            }
            _label.Content = displayText;

            // CRITICAL FIX: Calculate height immediately without coroutines
            // This allows height calculation even when the UI element is disabled by viewport culling
            CalculateAndSetHeightImmediate(displayText);

            // Set type indicator color based on message type and status
            if (_entry.Type == ConversationEntry.MessageType.Tool)
            {
                // For tool messages, use PillInfo as base but change color based on status
                var baseStyle = Style.Load<ImageStyle>("PillInfo"); // Use blue pill as base

                if (baseStyle != null && _typeIndicator != null)
                {
                    _typeIndicator.Sprite = baseStyle.sprite;
                    _typeIndicator.PixelDensityMultiplier = baseStyle.pixelDensityMultiplier;

                    // Set color based on tool status
                    _typeIndicator.Color = _entry.Status switch
                    {
                        ConversationEntry.ToolStatus.Success => new Color(0.2f, 0.8f, 0.2f, 1f), // Green
                        ConversationEntry.ToolStatus.Failure => new Color(0.9f, 0.2f, 0.2f, 1f), // Red
                        _ => new Color(1f, 0.6f, 0f, 1f) // Orange for pending
                    };
                }
                else
                {
                    // Base pill style or type indicator is null
                }
            }
            else if (_entry.Type == ConversationEntry.MessageType.System &&
                     _entry.Message.StartsWith("[Error:"))
            {
                var baseStyle = Style.Load<ImageStyle>("PillInfo");
                if (baseStyle != null && _typeIndicator != null)
                {
                    _typeIndicator.Sprite = baseStyle.sprite;
                    _typeIndicator.Color = new Color(0.9f, 0.2f, 0.2f, 1f);
                    _typeIndicator.PixelDensityMultiplier = baseStyle.pixelDensityMultiplier;
                }
            }
            else
            {
                // For non-tool messages, use standard PillInfo (blue)
                var indicatorStyle = Style.Load<ImageStyle>("PillInfo");
                if (indicatorStyle != null && _typeIndicator != null)
                {
                    _typeIndicator.Sprite = indicatorStyle.sprite;
                    _typeIndicator.Color = indicatorStyle.color;
                    _typeIndicator.PixelDensityMultiplier = indicatorStyle.pixelDensityMultiplier;
                }
            }
        }

        /// <summary>
        /// This method works even when the UI element is disabled by viewport culling.
        /// </summary>
        private void CalculateAndSetHeightImmediate(string displayText)
        {
            // Use predefined layout constants instead of relying on UI measurements
            var availableWidth = 718f;

            // Get text properties from the label's TextStyle (works even when disabled)
            var font = _label.TextStyle.font;
            var fontSize = _label.TextStyle.fontSize;

            // Calculate raw text height using TextGenerator (independent of UI state)
            var textHeight = CalculateTextHeight(displayText, font, fontSize, availableWidth);
            textHeight += 12.0f;
            SetHeight(textHeight);
            var currentSize = RectTransform.sizeDelta;
            RectTransform.sizeDelta = new Vector2(currentSize.x, textHeight);
        }

        private float CalculateTextHeight(string text, Font font, int fontSize, float width)
        {
            if (string.IsNullOrEmpty(text) || font == null || width <= 0)
                return fontSize;

            var textGenerator = new TextGenerator();
            var generationSettings = new TextGenerationSettings
            {
                font = font,
                fontSize = fontSize,
                lineSpacing = 1f,
                richText = false,
                color = Color.white,
                fontStyle = FontStyle.Normal,
                textAnchor = TextAnchor.UpperLeft,
                alignByGeometry = false,
                resizeTextForBestFit = false,
                resizeTextMinSize = fontSize,
                resizeTextMaxSize = fontSize,
                updateBounds = true,
                verticalOverflow = VerticalWrapMode.Overflow,
                horizontalOverflow = HorizontalWrapMode.Wrap,
                generateOutOfBounds = false,
                scaleFactor = 1f,
                pivot = Vector2.zero
            };

            // Set the generation area
            generationSettings.generationExtents = new Vector2(width, 0f);

            // Generate the text and get the height
            textGenerator.Populate(text, generationSettings);
            var calculatedHeight = textGenerator.GetPreferredHeight(text, generationSettings);
            return calculatedHeight;
        }

        /// <summary>
        /// Refresh the content display (useful for live transcription updates)
        /// </summary>
        internal void RefreshContent()
        {
            if (_entry != null)
            {
                UpdateDisplay();

                // Handle blinking cursor for live transcription
                if (_entry.IsLiveTranscription)
                {
                    StartBlinkingCursor();
                }
                else
                {
                    StopBlinkingCursor();
                }
            }
        }

        /// <summary>
        /// Start the blinking cursor animation for live transcription
        /// </summary>
        private void StartBlinkingCursor()
        {
            StopBlinkingCursor(); // Stop any existing cursor animation
            _blinkingCursorCoroutine = StartCoroutine(BlinkingCursorCoroutine());
        }

        /// <summary>
        /// Stop the blinking cursor animation
        /// </summary>
        private void StopBlinkingCursor()
        {
            if (_blinkingCursorCoroutine != null)
            {
                StopCoroutine(_blinkingCursorCoroutine);
                _blinkingCursorCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine that handles the blinking cursor effect
        /// </summary>
        private IEnumerator BlinkingCursorCoroutine()
        {
            bool showCursor = true;
            string baseText = _entry?.Message ?? "";

            while (_entry != null && _entry.IsLiveTranscription)
            {
                // Update display text with or without cursor
                var displayText = baseText + (showCursor ? "_" : "");
                if (_entry.HasTruncatedContent)
                {
                    displayText += "...";
                }
                _label.Content = displayText;

                showCursor = !showCursor;
                yield return new WaitForSeconds(CURSOR_BLINK_INTERVAL);
            }

            // Ensure cursor is removed when transcription is done
            var finalText = _entry?.Message ?? "";
            if (_entry?.HasTruncatedContent == true)
            {
                finalText += "...";
            }
            _label.Content = finalText;
        }

        /// <summary>
        /// Clean up when the object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            StopBlinkingCursor();
        }

        /// <summary>
        /// Handler for the OnPointerClick event, when clicked the conversation will display full message details.
        /// </summary>
        public override void OnPointerClick() => Entry?.DisplayDetails();
    }

    internal class ProxyConversationLine : ProxyController<ConversationLine>
    {
        internal ConversationEntry Entry { get; set; }

        protected override void Fill()
        {
            Target.Entry = Entry;
        }
    }
}
