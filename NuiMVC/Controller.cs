using System;
using System.Collections.Generic;
using Anvil.API;
using Anvil.API.Events;


namespace NuiMVC;

public abstract class ControllerBase
{
    private sealed class UIControllerException(string msg) : Exception(msg) { }

    private readonly NuiWindowToken token;


    protected void SetValue<T>(NuiBind<T> property, T value) => token.SetBindValue(property, value);
    protected void SetValues<T>(NuiBind<T> property, params T[] values) => token.SetBindValues(property, values);

    protected T GetValue<T>(NuiBind<T> property) => token.GetBindValue(property) ?? throw new UIControllerException($"Property \'{property.Key}\' value is null.");
    protected List<T> GetValues<T>(NuiBind<T> property) => token.GetBindValues(property) ?? throw new UIControllerException($"Property \'{property.Key}\' does not hold a list of values.");

    protected void SetWatch<T>(NuiBind<T> property, bool watch){ token.SetBindWatch(property, watch); }

    protected ControllerBase(NwPlayer player, NuiWindow window)
    {
        if (string.IsNullOrEmpty(window.Id))
            throw new UIControllerException("NuiWindow.Id is null or empty.");

        string windowId = window.Id;

        if (!player.TryCreateNuiWindow(window, out token, windowId))
            throw new UIControllerException($"Failed to create NuiWindow \'{windowId}\' for player {player.PlayerName}");

        token.OnNuiEvent += Handler;
    }

    /// <summary>
    /// Called when the window is being closed. It will carry a 'result object' (if any) obtained via <see cref="OnClose"/>
    /// </summary>
    public event Action<ControllerBase, object?>? ClosedEvent = null;

    /// <summary>
    /// Closes the UI window.
    /// </summary>
    private bool isClosed = false; // prevents double-deletion, potential segfault fix (?)
    public void Close()
    {
        if (isClosed) return;
        isClosed = true;

        token.OnNuiEvent -= Handler;
        token.Close();
        ClosedEvent?.Invoke(this, OnClose());
    }
    
    /// <summary>
    /// Obtain a result object (if any) from the model upon being closed.
    /// </summary>
    /// <returns>Result of player interaction with the window</returns>
    protected abstract object? OnClose();

    /// <summary>
    /// Callback function for button clicks
    /// </summary>
    /// <param name="elementId">ID of the view's button. It's recommended to use 'nameof(buttonName)' to avoid hardcoded values.</param>
    protected virtual void OnClick(string elementId){}

    /// <inheritdoc cref="OnClick(string)"/>
    /// <param name="arrayIndex">Index into an array of buttons, for example a NuiList.</param>
    /// <remarks>If not overriden in derived class, the second parameter is ignored and <see cref="OnClick(string)"/> will be called.</remarks>
    protected virtual void OnClick(string elementId, int arrayIndex) { OnClick(elementId); }

    /// <summary>
    /// Callback function for mouse-up events (clicking on images for example)
    /// </summary>
    /// <remarks>Element has to be enabled to send this event</remarks>
    /// <param name="elementId">ID of the view's element which was clicked. It's recommended to use 'nameof(elementName)' to avoid hardcoded values.</param>
    protected virtual void OnMouseUp(string elementId){}

    /// <inheritdoc cref="OnMouseUp(string)"/>
    /// <param name="arrayIndex">Index into an array of elements, for example a NuiList.</param>
    /// <remarks>If not overriden in derived class, the second parameter is ignored and <see cref="OnMouseUp(string)"/> will be called.</remarks>
    protected virtual void OnMouseUp(string elementId, int arrayIndex) { OnMouseUp(elementId); }

    /// <summary>
    /// Callback function for mouse-down events (clicking on images for example)
    /// </summary>
    /// <remarks>Element has to be enabled to send this event</remarks>
    /// <param name="elementId">ID of the view's element which was clicked. It's recommended to use 'nameof(elementName)' to avoid hardcoded values.</param>
    protected virtual void OnMouseDown(string elementId){}

    /// <inheritdoc cref="OnMouseDown(string)"/>
    /// <param name="arrayIndex">Index into an array of elements, for example a NuiList.</param>
    /// <remarks>If not overriden in derived class, the second parameter is ignored and <see cref="OnMouseDown(string)"/> will be called.</remarks>
    protected virtual void OnMouseDown(string elementId, int arrayIndex) { OnMouseDown(elementId); }

    /// <summary>
    /// Update other properties upon some property change
    /// </summary>
    /// <param name="elementId">ID of the property. It's recommended to use 'nameof(propertyName)' to avoid hardcoded values.</param>
    protected virtual void Update(string elementId){}

    /// <inheritdoc cref="Update(string)"/>
    /// <param name="arrayIndex">Index of the element in the array of NuiElements, which holds the property</param>
    /// <remarks>If not overriden in derived class, the second parameter is ignored and <see cref="Update(string)"/> will be called.</remarks>
    protected virtual void Update(string elementId, int arrayIndex) { Update(elementId); }


    private void Handler(ModuleEvents.OnNuiEvent eventData)
    {
        if (token != eventData.Token) throw new UIControllerException("Token mismatch.");

        switch (eventData.EventType)
        {
            case NuiEventType.Close:
                {
                    token.OnNuiEvent -= Handler;
                    ClosedEvent?.Invoke(this, OnClose());
                    break;
                }

            case NuiEventType.MouseUp:
                {
                    var elementId = eventData.ElementId;

                    if (string.IsNullOrEmpty(elementId))
                        throw new UIControllerException("OnNuiEvent.OnMouseUp data ElementId is null or empty");

                    int arrId = eventData.ArrayIndex;

                    if (arrId < 0) OnMouseUp(elementId);
                    else OnMouseUp(elementId, arrId);
                    break;
                }

            case NuiEventType.MouseDown:
                {
                    var elementId = eventData.ElementId;

                    if (string.IsNullOrEmpty(elementId))
                        throw new UIControllerException("OnNuiEvent.OnMouseDown data ElementId is null or empty");

                    int arrId = eventData.ArrayIndex;

                    if (arrId < 0) OnMouseDown(elementId);
                    else OnMouseDown(elementId, arrId);
                    break;
                }

            case NuiEventType.Click:
                {
                    var elementId = eventData.ElementId;

                    if (string.IsNullOrEmpty(elementId))
                        throw new UIControllerException("OnNuiEvent.Click data ElementId is null or empty");

                    int arrId = eventData.ArrayIndex;

                    if (arrId < 0) OnClick(elementId);
                    else OnClick(elementId, arrId);
                    break;
                }

            case NuiEventType.Watch:
                {
                    var elementId = eventData.ElementId;

                    if (string.IsNullOrEmpty(elementId))
                        throw new UIControllerException("OnNuiEvent.Watch data ElementId is null or empty");

                    int arrId = eventData.ArrayIndex;

                    if (arrId < 0) Update(elementId);
                    else Update(elementId, arrId);
                    break;
                }

        }
    }

}