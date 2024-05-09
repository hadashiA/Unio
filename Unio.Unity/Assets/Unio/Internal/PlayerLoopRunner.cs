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
            for (var i = 0; i < loopItems.LastIndex; i++)
            {
                var result = loopItems[i]?.MoveNext();
                if (result == false)
                {
                    loopItems.RemoveAt(i);
                }
            }
        }
    }
}