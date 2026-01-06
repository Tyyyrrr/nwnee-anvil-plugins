using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuestEditor.StageNode;

public partial class StageNodeView : UserControl
{
    public StageNodeView()
    {
        InitializeComponent();
    }

    bool drag = false;
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        drag = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        drag = false;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if(!drag) return;

        DragToMousePosition();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if(!drag) return;

        DragToMousePosition();
    }

    void DragToMousePosition() // ugly but works. todo: find cleaner approach
    {
        if (TemplatedParent is not ContentPresenter tParent) return;
        if (VisualTreeHelper.GetParent(tParent) is not Canvas canvas) return;

        var pos = Mouse.GetPosition(canvas);

        if (this.DataContext is not StageNodeViewModel vm) return;

        vm.X = pos.X-10;
        vm.Y = pos.Y-10;
    }
}

