using BIA.DAL.Repositories;
using BIA.Entity.ViewModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIA.BLL.BLLServices
{
    public class BLLForbiddenWords
    {
        private static List<ForbiddenWords> _forbiddenWords = new List<ForbiddenWords>();
        private static readonly object _lock = new object();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static volatile bool _isLoaded = false;

        public async Task<List<ForbiddenWords>> GetForbiddenWordsAsync()
        {
            lock (_lock)
            {
                if (_isLoaded)
                {
                    return new List<ForbiddenWords>(_forbiddenWords);
                }
            }

            await _semaphore.WaitAsync();
            try
            {
                bool alreadyLoaded;
                lock (_lock)
                {
                    alreadyLoaded = _isLoaded;
                }

                if (!alreadyLoaded)
                {
                    var dal = new DALForbiddenWords();
                    var wordsFromDb = await dal.GetForbiddenWordsFromDBAsync();
                    lock (_lock)
                    {
                        _forbiddenWords = wordsFromDb ?? new List<ForbiddenWords>();
                        _isLoaded = true;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            lock (_lock)
            {
                return new List<ForbiddenWords>(_forbiddenWords);
            }
        }
    }
}
