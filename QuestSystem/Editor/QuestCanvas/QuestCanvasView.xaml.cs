using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuestEditor.StageNode;

namespace QuestEditor.QuestCanvas;

public partial class QuestCanvasView : UserControl
{
    public QuestCanvasView()
    {
        InitializeComponent();

        Console.WriteLine("QuestCanvasView initialized.");
    }

    public static readonly DependencyProperty AddStageCommandProperty =
        DependencyProperty.Register(
            nameof(AddStageCommand),
            typeof(ICommand),
            typeof(QuestCanvasView)
        );

    public ICommand AddStageCommand
    {
        get => (ICommand)GetValue(AddStageCommandProperty);
        set => SetValue(AddStageCommandProperty, value);
    }
    public static readonly DependencyProperty RemoveStageCommandProperty =
        DependencyProperty.Register(
            nameof(RemoveStageCommand),
            typeof(ICommand),
            typeof(QuestCanvasView)
        );

    public ICommand RemoveStageCommand
    {
        get => (ICommand)GetValue(RemoveStageCommandProperty);
        set => SetValue(RemoveStageCommandProperty, value);
    }
    public static readonly DependencyProperty OverlayCapturesInputProperty =
        DependencyProperty.Register(
            nameof(OverlayCapturesInput),
            typeof(bool),
            typeof(QuestCanvasView)
    );

    public bool OverlayCapturesInput
    {
        get => (bool)GetValue(OverlayCapturesInputProperty);
        set => SetValue(OverlayCapturesInputProperty, value);
    }


    private StageNodeViewModel? _stageNodeToDelete = null;
    public void OnOverlayMouseUp(object s, MouseButtonEventArgs e)
    {
        if(e.ChangedButton == MouseButton.Left)
        {
            Console.WriteLine("LMB ^");
            OverlayCanvas.Children.Clear();
            _stageNodeToDelete = null;
            DisableOverlayInput();
        }
    }

public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
{
    while (child != null)
    {
        if (child is T parent)
            return parent;

        child = VisualTreeHelper.GetParent(child);
    }

    return null;
}

    public void OnMainCanvasMouseUp(object s, MouseButtonEventArgs e)
    {
        if(e.ChangedButton == MouseButton.Left)
        {
            DisableOverlayInput();
            return;

        }

        Console.WriteLine("RMB ^");

        OverlayCanvas.Children.Clear();
        _stageNodeToDelete = null;
        var pos = e.GetPosition(OverlayCanvas);

        var origin = e.OriginalSource;

        Console.WriteLine("Original Source type: " + origin.GetType().Name);

        if(origin is not Canvas)
        {
            var element = origin as DependencyObject;

            StageNodeView? snv = null;

            while(element != null)
            {
                snv = element as StageNodeView;
                if(snv != null) break;
                element = VisualTreeHelper.GetParent(element);
            }

            if(snv == null || snv.DataContext is not StageNodeViewModel snvm) return;

            _stageNodeToDelete = snvm;
            var nmv = new NodeMenuView();
            nmv.QuestStageDeleted += RemoveQuestStage;
            Canvas.SetLeft(nmv,pos.X);
            Canvas.SetTop(nmv,pos.Y);
            OverlayCanvas.Children.Add(nmv);
            EnableOverlayInput();
            return;
        }

        var view = new CanvasMenuView();
        view.QuestStageAdded += AddQuestStage;
        Canvas.SetLeft(view, pos.X);
        Canvas.SetTop(view, pos.Y);
        OverlayCanvas.Children.Add(view);
        EnableOverlayInput();
    }


    void AddQuestStage(CanvasMenuView sender)
    {
        var point = Mouse.GetPosition(OverlayCanvas);
        sender.QuestStageAdded -= AddQuestStage;
        OverlayCanvas.Children.Clear();
        Console.WriteLine($"Adding quest stage (point {point})");
        AddStageCommand.Execute(point);
        DisableOverlayInput();
    }
    void RemoveQuestStage(NodeMenuView sender)
    {
        sender.QuestStageDeleted -= RemoveQuestStage;
        OverlayCanvas.Children.Clear();
        Console.WriteLine($"Removing quest stage ({_stageNodeToDelete?.StageID.ToString() ?? "null"})");
        RemoveStageCommand.Execute(_stageNodeToDelete);
        _stageNodeToDelete = null;
        DisableOverlayInput();
    }

    void EnableOverlayInput() => OverlayCapturesInput = true;
    void DisableOverlayInput() => OverlayCapturesInput = false;
}

