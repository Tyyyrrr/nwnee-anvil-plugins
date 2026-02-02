using System.Windows;
using System.Windows.Media;

namespace QuestEditor.Graph
{
    public class ConnectionOutputVM : ConnectionSocketVM
    {
        public int TargetID
        {
            get => _targetID;
            set => SetProperty(ref _targetID, value);
        } int _targetID;

        public ConnectionOutputVM(int sourceID, int targetID) : base(sourceID)
        {
            _targetID = targetID;
            SocketColorBrush = (SolidColorBrush)((App)Application.Current).Resources["OutputSocketBrush"];
        }
    }
}
