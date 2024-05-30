namespace Unio.Internal
{
    interface IPlayerLoopItem
    {
        bool MoveNext();
    }

    sealed class PlayerLoopRunner
    {
        readonly FreeList<IPlayerLoopItem> loopItems = new FreeList<IPlayerLoopItem>(8);

        public void Dispatch(IPlayerLoopItem item)
        {
            loopItems.Add(item);
        }

        public void Run()
        {
            var span = loopItems.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                var result = span[i]?.MoveNext();
                if (result == false)
                {
                    loopItems.RemoveAt(i);
                }
            }
        }
    }
}