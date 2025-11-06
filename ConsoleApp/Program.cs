using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace CSConsoleApp
{
    public static class Program
    {
        public static void Main()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            // Найти CSV в текущей директории или вложенных
            var csvFiles = Directory.GetFiles(currentDirectory, "*.csv", SearchOption.AllDirectories);
            if (!csvFiles.Any())
            {
                var initOutPath = Path.Combine(currentDirectory, "analysis.txt");
                File.WriteAllText(initOutPath, "No CSV files found in current directory or subdirectories. Place a CSV file and run again.", Encoding.UTF8);
                Console.WriteLine($"No CSV found. Created {initOutPath}");
                return;
            }

            var filePath = csvFiles.First();

            IReadOnlyList<MovieCredit> movieCredits = null;
            try
            {
                var parser = new MovieCreditsParser(filePath);
                movieCredits = parser.Parse();
            }
            catch (Exception)
            {
                Console.WriteLine("Не удалось распарсить csv");
                Environment.Exit(1);
            }

            var sb = new StringBuilder();

            // 1. Фильмы режиссера Steven Spielberg
            var spielbergMovies = movieCredits
                .Where(m => m.Crew.Any(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase)
                                             && string.Equals(c.Name, "Steven Spielberg", System.StringComparison.OrdinalIgnoreCase)))
                .Select(m => m.Title)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            sb.AppendLine("1. Фильмы режиссера Steven Spielberg:");
            foreach (var t in spielbergMovies) sb.AppendLine(t);
            sb.AppendLine();

            // 2. Персонажи Tom Hanks
            var tomHanksCharacters = movieCredits
                .SelectMany(m => m.Cast)
                .Where(c => string.Equals(c.Name, "Tom Hanks", System.StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Character)
                .Where(ch => !string.IsNullOrWhiteSpace(ch))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            sb.AppendLine("2. Персонажи Tom Hanks:");
            foreach (var ch in tomHanksCharacters) sb.AppendLine(ch);
            sb.AppendLine();

            // 3. Топ-5 фильмов по количеству актеров
            var top5ByCastSize = movieCredits
                .OrderByDescending(m => (m.Cast?.Count ?? 0))
                .Take(5)
                .Select(m => new { m.Title, CastCount = m.Cast?.Count ?? 0 })
                .ToList();

            sb.AppendLine("3. Топ-5 фильмов по количеству актеров:");
            foreach (var it in top5ByCastSize) sb.AppendLine($"{it.Title} - {it.CastCount}");
            sb.AppendLine();

            // 4. Топ-10 актеров по числу фильмов
            var top10Actors = movieCredits
                .SelectMany(m => m.Cast)
                .GroupBy(c => c.Name)
                .Select(g => new { Actor = g.Key, Movies = g.Select(x => x.CastId).Distinct().Count() })
                .OrderByDescending(x => x.Movies)
                .Take(10)
                .ToList();

            sb.AppendLine("4. Топ-10 актеров по числу фильмов:");
            foreach (var a in top10Actors) sb.AppendLine($"{a.Actor} - {a.Movies}");
            sb.AppendLine();

            // 5. Уникальные департаменты
            var departments = movieCredits
                .SelectMany(m => m.Crew)
                .Where(c => !string.IsNullOrWhiteSpace(c.Department))
                .Select(c => c.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            sb.AppendLine("5. Уникальные департаменты:");
            foreach (var d in departments) sb.AppendLine(d);
            sb.AppendLine();

            // 6. Фильмы, где Hans Zimmer был Original Music Composer
            var hansZimmerMovies = movieCredits
                .Where(m => m.Crew.Any(c => string.Equals(c.Name, "Hans Zimmer", System.StringComparison.OrdinalIgnoreCase)
                                           && string.Equals(c.Job, "Original Music Composer", System.StringComparison.OrdinalIgnoreCase)))
                .Select(m => m.Title)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            sb.AppendLine("6. Фильмы, где Hans Zimmer был Original Music Composer:");
            foreach (var t in hansZimmerMovies) sb.AppendLine(t);
            sb.AppendLine();

            // 7. Словарь MovieId -> Director
            var movieToDirector = movieCredits
                .Select(m => new
                {
                    m.MovieId,
                    Director = m.Crew.FirstOrDefault(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase))?.Name ?? string.Empty
                })
                .ToDictionary(x => x.MovieId, x => x.Director);

            sb.AppendLine("7. Словарь MovieId -> Director (часть):");
            foreach (var kv in movieToDirector.Take(20)) sb.AppendLine($"{kv.Key} => {kv.Value}");
            sb.AppendLine();

            // 8. Фильмы с Brad Pitt и George Clooney
            var pittClooneyMovies = movieCredits
                .Where(m => m.Cast.Any(c => string.Equals(c.Name, "Brad Pitt", System.StringComparison.OrdinalIgnoreCase))
                            && m.Cast.Any(c => string.Equals(c.Name, "George Clooney", System.StringComparison.OrdinalIgnoreCase)))
                .Select(m => m.Title)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            sb.AppendLine("8. Фильмы с Brad Pitt и George Clooney:");
            foreach (var t in pittClooneyMovies) sb.AppendLine(t);
            sb.AppendLine();

            // 9. Уникальных человек в департаменте Camera
            var cameraPeopleCount = movieCredits
                .SelectMany(m => m.Crew)
                .Where(c => string.Equals(c.Department, "Camera", System.StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => c.Id)
                .Count();

            sb.AppendLine($"9. Уникальных человек в департаменте Camera: {cameraPeopleCount}");
            sb.AppendLine();

            // 10. Люди в Titanic и в Cast и в Crew
            var titanic = movieCredits.FirstOrDefault(m => string.Equals(m.Title, "Titanic", System.StringComparison.OrdinalIgnoreCase));
            var titanicBoth = new List<string>();
            if (titanic != null)
            {
                var castIds = titanic.Cast.Select(c => c.Id).ToHashSet();
                titanicBoth = titanic.Crew.Where(c => castIds.Contains(c.Id)).Select(c => c.Name).Distinct().ToList();
            }

            sb.AppendLine("10. Люди в Titanic и в Cast и в Crew:");
            foreach (var n in titanicBoth) sb.AppendLine(n);
            sb.AppendLine();

            // 11. Топ-5 членов съемочной группы Quentin Tarantino
            var tarantinoMovies = movieCredits
                .Where(m => m.Crew.Any(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase)
                                             && string.Equals(c.Name, "Quentin Tarantino", System.StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var tarantinoCrewTop5 = tarantinoMovies
                .SelectMany(m => m.Crew)
                .GroupBy(c => new { c.Id, c.Name })
                .Select(g => new { Person = g.Key.Name, Count = tarantinoMovies.Count(tm => tm.Crew.Any(y => y.Id == g.Key.Id)) })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            sb.AppendLine("11. Топ-5 членов съемочной группы Quentin Tarantino:");
            foreach (var it in tarantinoCrewTop5) sb.AppendLine($"{it.Person} - {it.Count}");
            sb.AppendLine();

            // 12. Топ-10 пар актеров, которые чаще всего снимались вместе
            var pairCounts = new Dictionary<(int, int), int>();
            foreach (var m in movieCredits)
            {
                var ids = m.Cast.Select(c => c.Id).Distinct().ToList();
                for (int i = 0; i < ids.Count; i++)
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        var a = ids[i] < ids[j] ? (ids[i], ids[j]) : (ids[j], ids[i]);
                        pairCounts[a] = pairCounts.TryGetValue(a, out var v) ? v + 1 : 1;
                    }
            }

            var top10Pairs = pairCounts.OrderByDescending(p => p.Value).Take(10)
                .Select(p => new
                {
                    Id1 = p.Key.Item1,
                    Id2 = p.Key.Item2,
                    Count = p.Value,
                    Name1 = movieCredits.SelectMany(m => m.Cast).FirstOrDefault(c => c.Id == p.Key.Item1)?.Name ?? p.Key.Item1.ToString(),
                    Name2 = movieCredits.SelectMany(m => m.Cast).FirstOrDefault(c => c.Id == p.Key.Item2)?.Name ?? p.Key.Item2.ToString()
                }).ToList();

            sb.AppendLine("12. Топ-10 пар актеров, которые чаще всего снимались вместе:");
            foreach (var p in top10Pairs) sb.AppendLine($"{p.Name1} & {p.Name2} - {p.Count}");
            sb.AppendLine();

            // 13. Топ-5 членов съемочной группы по числу разных департаментов
            var diversity = movieCredits
                .SelectMany(m => m.Crew)
                .GroupBy(c => new { c.Id, c.Name })
                .Select(g => new { Person = g.Key.Name, DistinctDepartments = g.Select(x => x.Department).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().Count() })
                .OrderByDescending(x => x.DistinctDepartments)
                .Take(5)
                .ToList();

            sb.AppendLine("13. Топ-5 членов съемочной группы по числу разных департаментов:");
            foreach (var it in diversity) sb.AppendLine($"{it.Person} - {it.DistinctDepartments}");
            sb.AppendLine();

            // 14. Творческие трио
            var creativeTrios = new List<string>();
            foreach (var m in movieCredits)
            {
                var byPerson = m.Crew.GroupBy(c => c.Id);
                foreach (var g in byPerson)
                {
                    var jobs = g.Select(x => x.Job).Where(j => !string.IsNullOrWhiteSpace(j)).Select(j => j.Trim()).ToHashSet(System.StringComparer.OrdinalIgnoreCase);
                    if (jobs.Contains("Director") && jobs.Contains("Writer") && jobs.Contains("Producer"))
                    {
                        var name = g.First().Name;
                        creativeTrios.Add($"{m.Title} - {name}");
                    }
                }
            }

            sb.AppendLine("14. Фильмы, где один человек был Director, Writer и Producer:");
            foreach (var s in creativeTrios) sb.AppendLine(s);
            sb.AppendLine();

            // 15. Два шага до Kevin Bacon
            var kevinIds = movieCredits.SelectMany(m => m.Cast).Where(c => string.Equals(c.Name, "Kevin Bacon", System.StringComparison.OrdinalIgnoreCase)).Select(c => c.Id).Distinct().ToHashSet();
            var step1 = movieCredits.Where(m => m.Cast.Any(c => kevinIds.Contains(c.Id))).SelectMany(m => m.Cast).Select(c => c.Id).Where(id => !kevinIds.Contains(id)).Distinct().ToHashSet();
            var step2 = movieCredits.Where(m => m.Cast.Any(c => step1.Contains(c.Id))).SelectMany(m => m.Cast).Select(c => c.Name).Where(n => !string.Equals(n, "Kevin Bacon", System.StringComparison.OrdinalIgnoreCase)).Distinct().OrderBy(n => n).ToList();

            sb.AppendLine("15. Актеры в двух шагах от Kevin Bacon:");
            foreach (var n in step2) sb.AppendLine(n);
            sb.AppendLine();

            // 16. Командная работа: средний размер Cast и Crew по режиссеру
            var byDirector = movieCredits
                .Select(m => new
                {
                    Movie = m,
                    Director = m.Crew.FirstOrDefault(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase))
                })
                .Where(x => x.Director != null)
                .GroupBy(x => x.Director.Name)
                .Select(g => new
                {
                    Director = g.Key,
                    AvgCast = g.Average(x => x.Movie.Cast?.Count ?? 0),
                    AvgCrew = g.Average(x => x.Movie.Crew?.Count ?? 0)
                })
                .OrderByDescending(x => x.AvgCast)
                .ToList();

            sb.AppendLine("16. Средний размер Cast и Crew по режиссерам (часть):");
            foreach (var d in byDirector.Take(30)) sb.AppendLine($"{d.Director} - AvgCast: {d.AvgCast:F2}, AvgCrew: {d.AvgCrew:F2}");
            sb.AppendLine();

            // 17. Универсалы и их самый частый департамент
            var allCrew = movieCredits.SelectMany(m => m.Crew).ToList();
            var allCast = movieCredits.SelectMany(m => m.Cast).ToList();
            var peopleBoth = allCrew.Select(c => c.Id).Distinct().Intersect(allCast.Select(c => c.Id).Distinct()).ToList();

            var universals = peopleBoth.Select(id => new
            {
                Id = id,
                Name = allCrew.FirstOrDefault(c => c.Id == id)?.Name ?? allCast.FirstOrDefault(c => c.Id == id)?.Name,
                TopDepartment = allCrew.Where(c => c.Id == id).GroupBy(c => c.Department).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
            }).Where(x => x.Name != null).ToList();

            sb.AppendLine("17. Универсалы и их самый частый департамент (часть):");
            foreach (var u in universals.Take(50)) sb.AppendLine($"{u.Name} - {u.TopDepartment}");
            sb.AppendLine();

            // 18. Пересечение элитных клубов
            var scorseseMovieIds = movieCredits.Where(m => m.Crew.Any(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase) && string.Equals(c.Name, "Martin Scorsese", System.StringComparison.OrdinalIgnoreCase))).Select(m => m.MovieId).ToHashSet();
            var nolanMovieIds = movieCredits.Where(m => m.Crew.Any(c => string.Equals(c.Job, "Director", System.StringComparison.OrdinalIgnoreCase) && string.Equals(c.Name, "Christopher Nolan", System.StringComparison.OrdinalIgnoreCase))).Select(m => m.MovieId).ToHashSet();

            var peopleWithScorsese = movieCredits
                .Where(m => scorseseMovieIds.Contains(m.MovieId))
                .SelectMany(m => m.Crew.Select(c => (Id: c.Id, Name: c.Name)).Concat(m.Cast.Select(c => (Id: c.Id, Name: c.Name))))
                .Distinct()
                .ToList();

            var peopleWithNolan = movieCredits
                .Where(m => nolanMovieIds.Contains(m.MovieId))
                .SelectMany(m => m.Crew.Select(c => (Id: c.Id, Name: c.Name)).Concat(m.Cast.Select(c => (Id: c.Id, Name: c.Name))))
                .Distinct()
                .ToList();

            var intersection = peopleWithScorsese.Select(p => p.Name).Intersect(peopleWithNolan.Select(p => p.Name)).OrderBy(n => n).ToList();

            sb.AppendLine("18. Люди, работавшие и с Scorsese, и с Nolan (имена, часть):");
            foreach (var n in intersection.Take(100)) sb.AppendLine(n);
            sb.AppendLine();

            // 19. Департаменты по среднему количеству актеров
            var deptToMovies = movieCredits
                .SelectMany(m => m.Crew.Select(c => new { Dept = c.Department, Movie = m }))
                .Where(x => !string.IsNullOrWhiteSpace(x.Dept))
                .GroupBy(x => x.Dept)
                .Select(g => new
                {
                    Dept = g.Key,
                    AvgCast = g.Select(x => x.Movie).Distinct().Average(m => m.Cast?.Count ?? 0)
                })
                .OrderByDescending(x => x.AvgCast)
                .ToList();

            sb.AppendLine("19. Департаменты по среднему количеству актеров в фильмах, где они работали (часть):");
            foreach (var d in deptToMovies.Take(50)) sb.AppendLine($"{d.Dept} - AvgCast: {d.AvgCast:F2}");
            sb.AppendLine();

            // 20. Архетипы персонажей Johnny Depp
            var johnnyRoles = movieCredits.SelectMany(m => m.Cast).Where(c => string.Equals(c.Name, "Johnny Depp", System.StringComparison.OrdinalIgnoreCase)).Select(c => c.Character).Where(ch => !string.IsNullOrWhiteSpace(ch)).ToList();
            var archetypes = johnnyRoles.Select(ch => ch.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty)
                .GroupBy(x => x)
                .Select(g => new { Word = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(50)
                .ToList();

            sb.AppendLine("20. Архетипы персонажей Johnny Depp (по первому слову):");
            foreach (var a in archetypes) sb.AppendLine($"{a.Word} - {a.Count}");
            sb.AppendLine();

            // Записываем файл
            var outPath = Path.Combine(currentDirectory, "analysis.txt");
            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

            Console.WriteLine($"Анализ завершен. Результаты в {outPath}");
        }
    }
}