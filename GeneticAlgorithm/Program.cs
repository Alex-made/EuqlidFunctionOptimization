﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common.Domain;
using Common.TestData;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;

namespace GeneticAlgorithm
{
	class Program
	{
		static void Main(string[] args)
		{
			TestData.GetData(out var servers, out var services);

			var chromosome = new MyChromosome(servers, services);

			var populationSize = 9;
			var population = new Population(populationSize, populationSize, chromosome);

			var fitness = new FuncFitness((c) =>
			{
				var mc = (MyChromosome) c;
				
				var genes = mc.GetGenes();
				var servers = genes.Select(x => (Server) x.Value).ToList();

				var freeServersTerm = servers.Count(x => x.IsFree) * 2.5;
				var negativeHddFreeTerm = servers.Where(x => x.HddFree < 0).Select(x=>x.HddFree * 3).Sum();
				var positiveHddFreeTerm = servers.Where(x => x.HddFree > 0).Select(x => x.HddFree * 0.2).Sum();
				var negativeRamFreeTerm = servers.Where(x => x.RamFree < 0).Select(x => x.RamFree * 3).Sum();
				var positiveRamFreeTerm = servers.Where(x => x.RamFree > 0).Select(x => x.RamFree * 0.2).Sum();
				var negativeCpuFreeTerm = servers.Where(x => x.CpuFree < 0).Select(x => x.CpuFree * 3).Sum();
				var positiveCpuFreeTerm = servers.Where(x => x.CpuFree > 0).Select(x => x.CpuFree * 0.2).Sum();

				var fitness = freeServersTerm + negativeHddFreeTerm + positiveHddFreeTerm + negativeRamFreeTerm +
				              positiveRamFreeTerm + negativeCpuFreeTerm + positiveCpuFreeTerm;

				//по каждому серверу нужно вычислить коэффициент заполненности
				var serversFill = new List<double>();
				foreach (var server in servers)
				{
					var serverFill = ((server.HddFull-server.HddFree) / server.HddFull) * 0.2 +
					((server.RamFull-server.RamFree) / server.RamFull) * 0.4 +
					((server.CpuFull-server.CpuFree) / server.CpuFull) * 0.4;
					serversFill.Add(serverFill);
				}
				serversFill = serversFill.OrderByDescending(x=>x).ToList();
				//отладочная строка
				//var serversFill = new List<double> { 0.9d, 0.9d, 0.6d, 0.2d,0 };
				//среднее значение заполненности серверов
				var averageFill = serversFill.Sum() / servers.Count();
				//рассчитаем дисперсию у serversFill
				var dispersion = serversFill.Select(eachFill => (eachFill - averageFill) * (eachFill - averageFill)).Sum()/servers.Count;

				return (dispersion*3) + fitness;
			});

			var selection = new EliteSelection();
			//сделал свой кроссовер, похожий на UniformCrossover
			var crossover = new MyCrossover();
			//строка для отладки кроссовера
			//crossover.Cross(new List<IChromosome> {chromosome.CreateNew(), chromosome.CreateNew()});

			var mutation = new MyMutation();
			//mutation.Mutate(chromosome, Single.Epsilon);

			var termination = new FitnessStagnationTermination(100);

			var ga = new GeneticSharp.Domain.GeneticAlgorithm(
				population,
				fitness,
				selection,
				crossover,
				mutation)
			{
				Termination = termination,
				MutationProbability = 0.3f
			};

			var latestFitness = 0.0;
			ga.GenerationRan += (sender, e) =>
			{
				var ga = (GeneticSharp.Domain.GeneticAlgorithm) sender;
				Console.Write("Номер итерации: " + ga.GenerationsNumber + "  " +"Время работы: " + ga.TimeEvolving);
				Console.WriteLine();

				var bestChromosome = ga.BestChromosome as MyChromosome;
				var bestFitness = bestChromosome.Fitness.Value;

				if (bestFitness != latestFitness)
				{
					latestFitness = bestFitness;
					var phenotype = bestChromosome.GetGenes()
						.Select(x => (Server)x.Value).ToList();

					Console.Write("Фитнес: " + bestFitness);
					Console.WriteLine();

					foreach (var server in phenotype)
					{
						Console.Write("Сервер: " + server.Name +
									  ", HddFull: " + server.HddFull +
									  ", RamFull: " + server.RamFull +
									  ", HddFree: " + server.HddFree +
									  ", RamFree: " + server.RamFree +
									  ", CpuFree: " + server.CpuFree +
									  ", сервисы: ");
						Console.Write(" Заполненность Hdd: " + Math.Round(100 - (server.HddFree/ server.HddFull * 100)) + "%");
						Console.Write(" Заполненность Ram: " + Math.Round(100 - (server.RamFree/ server.RamFull * 100)) + "%");
						Console.Write(" Заполненность Cpu: " + Math.Round(100 - server.CpuFree) + "%");
						foreach (var service in server.Services)
						{
							Console.Write(service.Name + ", ");
						}
						Console.WriteLine();
						Console.WriteLine();

						//получить хромосомы на этой итерации и смотреть, равны ли они. 
						//Хромосомы равны, если равны их фитнесы. 
						//Если равны, алгоритм преждевременно сходится (вырождается популяция)
						var chromosomesFitnessCollection = ga.Population.CurrentGeneration.Chromosomes;
					}
				}
			};

			ga.Start();
		}

		private void ShowResult(IChromosome chromosome, IFitness fitness)
		{
			for (var i = 0; i < 30; i++)
			{
				Console.Write(fitness.Evaluate(chromosome) + " ");
				foreach (var gene in chromosome.GetGenes())
				{
					var server = (Server)gene.Value;
					var serviceNames = server.Services.Select(x => x.Name.ToString()).ToList();
					Console.Write("Сервер - " + server.Name + ":{ ");
					serviceNames.ForEach(x => Console.Write(x + ";"));
					Console.Write("} ");
				}
				Console.WriteLine();
				Console.WriteLine("Next");
			}
		}
	}
}

//services = new List<Service>()
//			{
//				new Service("Service_1", OsType.Windows, 5, 1),
//				new Service("Service_2", OsType.Windows, 12, 2),
//				new Service("Service_3", OsType.Windows, 3, 1),
//				new Service("Service_4", OsType.Windows, 7, 2),
//				new Service("Service_Linux_1", OsType.Linux, 8, 2),
//				//new Service("Service_5", OsType.Windows, 2, 1),
//				//new Service("Service_6", OsType.Windows, 12, 2),
//				//new Service("Service_7", OsType.Windows, 13, 1),
//				//new Service("Service_8", OsType.Windows, 4, 2),
//				//new Service("Service_Linux_2", OsType.Linux, 2, 2),
//				//new Service("Service_9", OsType.Windows, 5, 1),
//				//new Service("Service_10", OsType.Windows, 12, 2),
//				//new Service("Service_11", OsType.Windows, 3, 1),
//				//new Service("Service_12", OsType.Windows, 7, 2),
//				//new Service("Service_Linux_3", OsType.Linux, 8, 2)
//			};
