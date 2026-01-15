using System;

namespace QuestSystem.Graph
{
    internal sealed partial class QuestGraph
    {
        private readonly struct PlayerCursor
        {
            public const int NONE = int.MinValue;
            public static PlayerCursor None = new(NONE);
            public readonly int Root;
            public readonly int Node;
            public bool IsAtRoot => Root == Node;
            public bool IsOnGraph => Root>=0 && Node>=0;
            public PlayerCursor(int root, int node){Root = root; Node = node;}
            public PlayerCursor(int root){Root = root; Node = root;}

            public static implicit operator PlayerCursor(int integer) => new(integer);
            public static implicit operator PlayerCursor((int,int) tuple) => new(tuple.Item1,tuple.Item2);

            public static explicit operator int(PlayerCursor cursor) => cursor.Root;
            public static implicit operator (int,int)(PlayerCursor cursor) =>(cursor.Root, cursor.Node);

            public static bool operator ==(PlayerCursor left, PlayerCursor right) => left.Root == right.Root && left.Node == right.Node;
            public static bool operator !=(PlayerCursor left, PlayerCursor right) => !(left == right);

            public override bool Equals(object? obj) => obj is PlayerCursor cursor && cursor == this;
            public override int GetHashCode() => HashCode.Combine(Root,Node);
        }
    }
}