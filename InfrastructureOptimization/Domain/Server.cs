﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureOptimization.Domain;

namespace DatacenterEnvironmentSimulator.Models
{
	public class Server
	{
		private IList<Service> _services = new List<Service>();
		private float _hddFree;
		private float _ramFree;

		public Server(string name, OsType osType, float hddFull, float ramFull)
		{
			Name = name;
			Os = osType;
			HddFull = hddFull;
			RamFull = ramFull;

			_hddFree = HddFull;
			_ramFree = RamFull;
			IsFree = true;
		}

		public bool IsFree { get; private set; }
		public string Name { get; set; }
		public OsType Os { get; set; }
		public float HddFull { get; set; }

		public float HddFree => _hddFree;

		public float RamFull { get; set; }
		public float RamFree => _ramFree;

		public IReadOnlyCollection<Service> Services
		{
			get => new ReadOnlyCollection<Service>(_services);
		}

		public void AddService(Service service)
		{
			IsFree = false;
			_hddFree -= service.Hdd;
			_ramFree -= service.Ram;
			_services.Add(service);
		}
	}
}
