using System.Windows;
using System.Windows.Controls;

namespace QuestEditor.Objectives
{

    public class ObjectiveControl : ContentControl
    {
        static ObjectiveControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveControl), new FrameworkPropertyMetadata(typeof(ObjectiveControl)));
        }
    }
    public sealed class ObjectiveInteractControl : ObjectiveControl
    {
        static ObjectiveInteractControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveInteractControl), new FrameworkPropertyMetadata(typeof(ObjectiveInteractControl)));
        }
    }
    public sealed class ObjectiveKillControl : ObjectiveControl
    {
        static ObjectiveKillControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveKillControl), new FrameworkPropertyMetadata(typeof(ObjectiveKillControl)));
        }
    }
    public sealed class ObjectiveDeliverControl : ObjectiveControl
    {
        static ObjectiveDeliverControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveDeliverControl), new FrameworkPropertyMetadata(typeof(ObjectiveDeliverControl)));
        }
    }
    public sealed class ObjectiveObtainControl : ObjectiveControl
    {
        static ObjectiveObtainControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveObtainControl), new FrameworkPropertyMetadata(typeof(ObjectiveObtainControl)));
        }
    }
    public sealed class ObjectiveExploreControl : ObjectiveControl
    {
        static ObjectiveExploreControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveExploreControl), new FrameworkPropertyMetadata(typeof(ObjectiveExploreControl)));
        }
    }
    public sealed class ObjectiveSpellcastControl : ObjectiveControl
    {
        static ObjectiveSpellcastControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ObjectiveSpellcastControl), new FrameworkPropertyMetadata(typeof(ObjectiveSpellcastControl)));
        }
    }
}
