using System;
using Anvil.API;

namespace CharacterAppearance.Wrappers
{
    internal abstract class ItemWrapper
    {
        private sealed class ItemWrapperException : Exception
        {
            private static readonly string _msg = "Wrapped item is not valid";
            public ItemWrapperException():base(_msg){}
        }

        private NwItem? _item = null;
        public bool HasItem => _item != null && _item.IsValid && _item.TryGetUUID(out Guid guid) && guid != Guid.Empty;

        public virtual NwItem Item
        {
            get => HasItem ? _item! : throw new ItemWrapperException();
            set => _item = value;
        }

        private Guid _originalGuid = Guid.Empty;
        public Guid OriginalGuid
        {
            get => HasItem ? _originalGuid : Guid.Empty;
            set {
                if(_originalGuid != value)
                {
                    _originalGuid = value;
                    OriginalGuidString = _originalGuid.ToUUIDString();
                } 
            }
        }

        public abstract void RestoreOriginal();

        public virtual void ClearItem()
        {
            _item = null;
            OriginalGuid = Guid.Empty;
        }
        public virtual void MarkAsOriginal()
        {
            if (!HasItem)
            {
                ClearItem();
                return;
            }
            
            OriginalGuid = Item.UUID;
        }


        public string OriginalGuidString {get;private set;} = Guid.Empty.ToUUIDString();

        public bool IsGuidDirty => HasItem && (!Item.TryGetUUID(out var guid) || guid != OriginalGuid);

        protected void RestoreOriginalUUID() 
        { 
            if(HasItem) 
                NWN.Core.NWNX.ObjectPlugin.ForceAssignUUID(Item.ObjectId, OriginalGuidString); 
        }
    }
}