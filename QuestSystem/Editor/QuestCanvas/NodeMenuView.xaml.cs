using System;
using System.Windows.Controls;

namespace QuestEditor.QuestCanvas;

public partial class NodeMenuView : UserControl
{
    public NodeMenuView()
    {
        InitializeComponent();
        
    }

    void OnQuestStageDeleted(object s, EventArgs e)
    {
        QuestStageDeleted?.Invoke(this);
    }

    public event Action<NodeMenuView>? QuestStageDeleted;
}

