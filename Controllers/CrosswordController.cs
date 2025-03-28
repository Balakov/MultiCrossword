﻿using Crossword.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Crossword.Controllers
{
    public class CrosswordController : Controller
    {
        private Random m_random = new Random();
        private Storage.BoardStorageService m_boardStorageService;

        public CrosswordController(Storage.BoardStorageService boardStorageService)
        {
            m_boardStorageService = boardStorageService;
        }

        public class IndexViewModel
        {
            public CrosswordData Data { get; set; }
            public BoardCell[,] CellArray { get; set; }
            public string PlayerId { get; set; }
            public string GameId { get; set; }
            public DateTime CompletionDate { get; set; }
            public int CompletionTimeInSeconds { get; set; }
            public bool Debug { get; set; }
        }

        private static string GetNextCellId(int currentX, int currentY, string word, int indexAlongWord, string direction)
        {
            if (indexAlongWord < word.Length - 1)
            {
                if (direction == "across")
                {
                    currentX++;
                }
                else
                {
                    currentY++;
                }
            }

            return "#" + Storage.BoardStorage.CellIdKey(currentX, currentY);
        }

        private static string GetPreviousCellId(int currentX, int currentY, string word, int indexAlongWord, string direction)
        {
            if (indexAlongWord > 0)
            {
                if (direction == "across")
                {
                    currentX--;
                }
                else
                {
                    currentY--;
                }
            }

            return "#" + Storage.BoardStorage.CellIdKey(currentX, currentY);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string url, bool debug = false)
        {
            IndexViewModel vm = new IndexViewModel()
            {
                PlayerId = m_random.Next().ToString(),
                Debug = debug
            };

            // See if we have cached this board locally
            string gameJSON = await m_boardStorageService.GetBoardJSONAsync(url);
            Storage.BoardStorage completedState = null;

            if (gameJSON == null)
            {
                // Download and cache
                gameJSON = await CrosswordDownloader.DownloadAsync(url);
                if (gameJSON != null)
                {
                    await m_boardStorageService.SetBoardJSONAsync(url, gameJSON);
                    completedState = new Storage.BoardStorage();
                }
            }

            if (gameJSON != null)
            {
                vm.Data = JsonSerializer.Deserialize<CrosswordJson>(gameJSON)?.data;
                vm.GameId = Storage.BoardStorage.GameIdKey(vm.Data.crosswordType, vm.Data.number.ToString());

                if(completedState == null)
                {
                    // Do we have a completed state for this board?
                    var existingCompletedState = await m_boardStorageService.GetBoardStateCompleteAsync(vm.GameId);
                    if (existingCompletedState?.Cells.Any() != true)
                    {
                        // Write out a new completed state
                        completedState = new Storage.BoardStorage();
                    }
                }

                Storage.BoardStorageService.CrosswordCompletionInfo completionInfo = await m_boardStorageService.GetCompletionTime(vm.GameId);
                if (completionInfo != null)
                {
                    vm.CompletionTimeInSeconds = completionInfo.Seconds;
                    vm.CompletionDate = completionInfo.Date;
                }

                var existingBoard = await m_boardStorageService.GetBoardStateAsync(vm.GameId);

                vm.CellArray = new BoardCell[vm.Data.dimensions.cols, vm.Data.dimensions.rows];
                foreach (var entry in vm.Data.entries)
                {
                    int x = entry.position.x;
                    int y = entry.position.y;

                    for (int i = 0; i < entry.length; i++)
                    {
                        List<int> separatorLocations = new List<int>();
                        if (entry.separatorLocations != null)
                        {
                            foreach (var pair in entry.separatorLocations)
                            {
                                separatorLocations.AddRange(pair.Value);
                            }
                        }

                        if (vm.CellArray[x, y] == null)
                        {
                            existingBoard.Cells.TryGetValue(Storage.BoardStorage.CellIdKey(x, y), out var cellValue);

                            vm.CellArray[x, y] = new BoardCell()
                            {
                                X = x,
                                Y = y,
                                Number = (i == 0) ? entry.number : 0, // Only on initial letter
                                Value = cellValue?.Value,
                                Solution = entry.solution[i].ToString().ToUpper(),
                                ClueIdByDirection = new Dictionary<string, Navigation>()
                                {
                                    {
                                        entry.direction, new Navigation()
                                        {
                                            ClueId = entry.id,
                                            NextCellId = GetNextCellId(x, y, entry.solution, i, entry.direction),
                                            PreviousCellId = GetPreviousCellId(x, y, entry.solution, i, entry.direction),
                                            HasSeparator = separatorLocations.Any(x => x == i+1)
                                        }
                                    }
                                }
                            };

                            if (completedState != null)
                            {
                                completedState.Cells.Add(Storage.BoardStorage.CellIdKey(x, y), new Storage.BoardStorage.CellData() { Value = vm.CellArray[x, y].Solution });
                            }
                        }
                        else
                        {
                            if (i == 0)
                            {
                                vm.CellArray[x, y].Number = entry.number;
                            }

                            vm.CellArray[x, y].ClueIdByDirection.Add(entry.direction, new Navigation()
                            {
                                ClueId = entry.id,
                                NextCellId = GetNextCellId(x, y, entry.solution, i, entry.direction),
                                PreviousCellId = GetPreviousCellId(x, y, entry.solution, i, entry.direction),
                                HasSeparator = separatorLocations.Any(x => x == i + 1)
                            });
                        }

                        if (entry.direction == "across")
                        {
                            x++;
                        }
                        else
                        {
                            y++;
                        }
                    }
                }

                if (completedState != null)
                {
                    await m_boardStorageService.SetBoardStateCompleteAsync(vm.GameId, completedState);
                }
            }

            return View(vm);
        }

    }
}
