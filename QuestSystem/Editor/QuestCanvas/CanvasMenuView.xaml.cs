using System;
using System.Windows.Controls;

namespace QuestEditor.QuestCanvas;

public partial class CanvasMenuView : UserControl
{
    public CanvasMenuView()
    {
        InitializeComponent();
        
    }

    void OnQuestStageAdded(object s, EventArgs e)
    {
        QuestStageAdded?.Invoke(this);
    }

    public event Action<CanvasMenuView>? QuestStageAdded;
}

