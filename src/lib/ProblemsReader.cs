﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using lib.Web;
using log4net;

namespace lib
{
    public class ProblemsReader
    {
		public static IEnumerable<MyProblem> ProblemsToSolve()
		{
			var problems = File
				.ReadAllLines(@"../../../../problems.txt")
				.Select(l => l.Trim())
				.Where(l => l.Length > 0)
				.Select(l => l.Split('\t'))
				.Select(elms => new { Id = elms[0], Size = elms[1], Solved = elms[2], Bonus = elms[3], Operations = elms[4].Split(',').Select(t => t.Trim()).ToArray() })
				.Select(d => new MyProblem
				{
					Id = d.Id,
					Size = int.Parse(d.Size),
					Tried = d.Solved.Length > 0,
					Solved = d.Solved.Equals("True", StringComparison.OrdinalIgnoreCase),
					Bonus = d.Bonus.Equals("Bonus", StringComparison.OrdinalIgnoreCase),
					Operations = d.Operations,
				})
				.ToList();
			return problems.Where(p => !p.Solved && !p.Bonus).ToArray();
		}


        public static ILog log;

        public string ProblemsFilename;
        public string ProblemsResultsPath;
        public string TrainProblemsPath;
        public string TrainProblemsResultsPath;

		public static ProblemsReader CreateDefault()
		{
			return new ProblemsReader
			{
				ProblemsFilename = @"..\..\..\..\problems.txt",
				ProblemsResultsPath = @"..\..\..\..\problems-results\",
				TrainProblemsPath = @"..\..\..\..\problems-samples\",
				TrainProblemsResultsPath = @"..\..\..\..\problems-samples\"
			};

		}
        public string TrainProblemResultFilename(TrainResponse trainProblem, Problem problem)
        {
            bool isRelevant = trainProblem.operators.OrderBy(t => t).SequenceEqual(problem.AllOperators.OrderBy(t => t));
            string filename = String.Format(isRelevant ? "{0}_{1}.result" : "{0}_{1}.unrelevant.result", problem.Id, trainProblem.id);
            return Path.Combine(TrainProblemsResultsPath, filename);
        }

        public string TrainProblemFilename(TrainResponse trainProblem, Problem problem)
        {
            bool isRelevant = trainProblem.operators.OrderBy(t => t).SequenceEqual(problem.AllOperators.OrderBy(t => t));
            string filename = String.Format(isRelevant ? "{0}.txt" : "{0}.unrelevant.txt", problem.Id);
            return Path.Combine(TrainProblemsPath, filename);
        }

        public string ProblemResultFilename(Problem problem)
        {
            string filename = String.Format("{0}.result", problem.Id);
            return Path.Combine(ProblemsResultsPath, filename);
        }

        public Problem[] ReadProblems()
        {
            string path = ProblemsFilename;
            var problemsText = File.ReadAllLines(path).Where(line => line.Trim().Length > 0);
            Problem[] problems = problemsText.Select(Problem.Parse).Where(p => p != null).ToArray();
            Log("Readed file '{0}'. {1} problems loaded.", path, problems.Count());
            return problems;
        }

        public TrainResponse[] ReadTrainProblems(Problem associatedProblem, bool isRelevant)
        {
            try
            {
                string relevant = Path.Combine(TrainProblemsPath, String.Format("{0}.txt", associatedProblem.Id));
                string unrelevant = Path.Combine(TrainProblemsPath, String.Format("{0}.unrelevant.txt", associatedProblem.Id));
                string path = isRelevant ? relevant : unrelevant;
                string[] problemsText = File.ReadAllLines(path);
                TrainResponse[] trainProblems = problemsText.Select(TrainResponse.Parse).Where(p => p != null).ToArray();
                Log("Readed file '{0}'. {1} train problems loaded.", path, trainProblems.Count());
                return trainProblems;
            }
            catch (Exception)
            {
                return new TrainResponse[0];
            }
        }

        public Dictionary<UInt64, UInt64> ReadResultsForProblem(Problem problem)
        {
            string path = ProblemResultFilename(problem);
            return ReadResults(path);
        }

        public Dictionary<UInt64, UInt64> ReadResultsForTrainProblem(TrainResponse trainProblem, Problem problem)
        {
            string path = TrainProblemResultFilename(trainProblem, problem);
            return ReadResults(path);
        }

        public void SaveResultsForProblem(Problem problem, Dictionary<UInt64, UInt64> results, bool rewrite = false)
        {
            string path = ProblemResultFilename(problem);
            SaveResults(path, results, rewrite);
        }

        public void SaveResultsForTrainProblem(TrainResponse trainProblem, Problem problem, IDictionary<UInt64, UInt64> results, bool rewrite =false)
        {
            string path = TrainProblemResultFilename(trainProblem, problem);
            SaveResults(path, results, rewrite);
        }

        public void SaveTrainProblem(TrainResponse trainProblem, Problem problem)
        {
            File.AppendAllText(TrainProblemFilename(trainProblem, problem),
                                   trainProblem + Environment.NewLine);
        }

        private Dictionary<UInt64, UInt64> ReadResults(string path)
        {
            try
            {
                Dictionary<UInt64, UInt64> res = new Dictionary<ulong, ulong>();
                var results = File.ReadAllLines(path);
                foreach (var result in results)
                {
                    try
                    {
                        var xy = result.Split('\t').Select(i => i.Trim()).ToArray();
                        var x = UInt64.Parse(xy[0]);
                        var y = UInt64.Parse(xy[1]);
                        res[x] = y;
                    }
                    catch (Exception ex)
                    {
                        Log("Cant parse result " + result + " in file " + path);
                    }

                }
                return res;
            }
            catch (Exception ex)
            {
                Log("Can't read results from file " + path + ". " + ex.Message);
                return new Dictionary<ulong, ulong>();
            }
        }

        private void SaveResults(string path, IDictionary<UInt64, UInt64> results, bool rewrite)
        {
            try
            {
                Dictionary<UInt64, UInt64> currentResults = rewrite
                    ? new Dictionary<UInt64, UInt64>()
                    : ReadResults(path);
                foreach (var result in results)
                    currentResults[result.Key] = result.Value;
                List<string> res = new List<string>();
                foreach (var xy in currentResults)
                    res.Add("" + xy.Key + "\t" + xy.Value + Environment.NewLine);
                File.WriteAllLines(path, res);
            }
            catch (Exception ex)
            {
                Log("Can't save results in file " + path + ". " + ex.Message);
            }

        }

        private static void Log(string text, params object[] values)
        {
            if (log != null)
                log.DebugFormat(text, values);
        }
    }

	public class MyProblem
	{
		public string Id;
		public int Size;
		public bool Tried;
		public bool Solved;
		public bool Bonus;
		public string[] Operations;
	}
}
