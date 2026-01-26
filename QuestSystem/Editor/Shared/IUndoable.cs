namespace QuestEditor.Shared
{
    public interface IUndoable 
    { 
        void Undo(); 
        void Redo(); 
    }
}
