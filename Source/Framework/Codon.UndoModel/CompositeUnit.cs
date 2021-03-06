﻿#region File and License Information
/*
<File>
	<License>
		Copyright © 2009 - 2017, Daniel Vaughan. All rights reserved.
		This file is part of Codon (http://codonfx.com), 
		which is released under the MIT License.
		See file /Documentation/License.txt for details.
	</License>
	<CreationDate>2010-02-06 18:16:45Z</CreationDate>
</File>
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Codon.UndoModel
{
	/// <summary>
	/// Provides the ability to group units for execution
	/// either sequentially or in parallel. 
	/// <see cref="CompositeUnit{T}.Parallel"/>
	/// </summary>
	public class CompositeUnit<T> : UnitBase<T>
	{
		readonly string descriptionForUser;
		readonly Dictionary<UnitBase<T>, T> unitDictionary;

		public CompositeUnit(IDictionary<UnitBase<T>, T> units, string descriptionForUser)
		{
			//AssertArg.IsNotNull(descriptionForUser, nameof(descriptionForUser));
			//AssertArg.IsNotNull(units, nameof(units));
			this.descriptionForUser = descriptionForUser ?? throw new ArgumentNullException(nameof(descriptionForUser));
			unitDictionary = new Dictionary<UnitBase<T>, T>(
				units ?? throw new ArgumentNullException(nameof(units)));

			Execute += OnExecute;

			/* Determine repeatable status. */
			bool repeatable = unitDictionary.Keys.Count > 0;
			foreach (IInternalUnit key in units.Keys)
			{
				if (!key.Repeatable)
				{
					repeatable = false;
				}
			}
			Repeatable = repeatable;
		}

		void OnExecute(object sender, UnitEventArgs<T> e)
		{
			ExecuteInternal(unitDictionary, e.UnitMode);
		}

		protected internal virtual void ExecuteInternal(
			Dictionary<UnitBase<T>, T> unitDictionary, UnitMode unitMode)
		{
			if (Parallel)
			{
				ExecuteInParallel(unitDictionary, unitMode);
			}
			else
			{
				ExecuteInSequence(unitDictionary, unitMode);
			}
		}

		public override string DescriptionForUser => descriptionForUser;

		#region Parallel Execution
		/// <summary>
		/// Gets or sets a value indicating whether the composite units
		/// are to be performed sequentially, one after the other, or in parallel.
		/// </summary>
		/// <value><c>true</c> if parallel; otherwise, <c>false</c>.</value>
		public bool Parallel { get; set; }

		static void ExecuteInSequence(
			Dictionary<UnitBase<T>, T> unitDictionary, UnitMode unitMode)
		{
			foreach (KeyValuePair<UnitBase<T>, T> pair in unitDictionary)
			{
				var unit = (IInternalUnit)pair.Key;
				unit.PerformUnit(pair.Value, unitMode);
			}
		}

		static async void ExecuteInParallel(
			Dictionary<UnitBase<T>, T> unitDictionary, UnitMode unitMode)
		{
			/* When we move to .NET 4 we may use System.Threading.Parallel for the Desktop CLR. */
			var performedUnits = new List<UnitBase<T>>();
			object performedUnitsLock = new object();
			var exceptions = new List<Exception>();
			object exceptionsLock = new object();
			var events = unitDictionary.ToDictionary(x => x, x => new AutoResetEvent(false));

			foreach (KeyValuePair<UnitBase<T>, T> pair in unitDictionary)
			{
				var autoResetEvent = events[pair];
				var unit = (IInternalUnit)pair.Key;
				var undoableUnit = pair.Key;
				var arg = pair.Value;

#pragma warning disable 4014
				Task.Run(
#pragma warning restore 4014
					delegate
					{
						try
						{
							unit.PerformUnit(arg, unitMode);
							lock (performedUnitsLock)
							{
								performedUnits.Add(undoableUnit);
							}
						}
						catch (Exception ex)
						{
							/* TODO: improve this to capture undone unit errors. */
							lock (exceptionsLock)
							{
								exceptions.Add(ex);
							}
						}
						autoResetEvent.Set();
					});

			}

			foreach (var autoResetEvent in events.Values)
			{
				autoResetEvent.WaitOne();
			}

			if (exceptions.Count > 0)
			{
				throw new AggregateException("Unable to undo units", exceptions);
			}
		}
		#endregion
	}
}
