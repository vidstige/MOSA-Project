﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mosa.Tools.Permutations
{
	public static class I8
	{
		public static string Type = "long";
		public static byte Bits = 64;

		private static IList<string> allPermutations = null;
		public static IList<string> AllPermutations { get { if (allPermutations == null) allPermutations = GetAllPermutations(); return allPermutations; } }

		public static IList<string> GetAllPermutations()
		{
			IList<string> list = new List<string>();

			list.AddIfNew(0.ToString());
			list.AddIfNew(1.ToString());
			//list.AddIfNew(2.ToString());
			list.AddIfNew(long.MinValue.ToString());
			list.AddIfNew(long.MaxValue.ToString());
			list.AddIfNew((long.MinValue + 1).ToString());
			list.AddIfNew((long.MaxValue - 1).ToString());
			list.AddIfNew(int.MinValue.ToString());
			list.AddIfNew(int.MaxValue.ToString());
			list.AddIfNew((int.MinValue + 1).ToString());
			list.AddIfNew((int.MaxValue - 1).ToString());

			//list.AddIfNew(17.ToString());
			//list.AddIfNew(123.ToString());

			// power of two numbers
			//list.AddIfNew(Power2.GetPowerTwos(0, (ulong)long.MaxValue));

			// a few prime numbers
			//list.AddIfNew(Prime.GetSomePrimes(0, (ulong)long.MaxValue));

			// Get negatives
			list.AddIfNew(Negative.GetNegatives(long.MinValue, list));

			return list;
		}

		public static IList<string> GetPermutationResults()
		{
			List<string> results = new List<string>();

			results.Add(NumericTemplates.CreateAdd(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateSub(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateMul(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateDiv(Type, AllPermutations, AllPermutations, false, true));

			results.Add(NumericTemplates.CreateRem(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateNeg(Type, AllPermutations, false));
			results.Add(NumericTemplates.CreateRet(Type, AllPermutations, false));

			results.Add(NumericTemplates.CreateAnd(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateOr(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateXor(Type, AllPermutations, AllPermutations, false));

			results.Add(NumericTemplates.CreateComp(Type, AllPermutations, false));

			results.Add(NumericTemplates.CreateShl(Type, AllPermutations, Upto.GetUpto(Bits), false));
			results.Add(NumericTemplates.CreateShr(Type, AllPermutations, Upto.GetUpto(Bits), false));

			results.Add(NumericTemplates.CreateCeq(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateCgt(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateClt(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateCge(Type, AllPermutations, AllPermutations, false));
			results.Add(NumericTemplates.CreateCle(Type, AllPermutations, AllPermutations, false));

			results.Add(ArrayTemplates.CreateNewarr(Type, false));
			results.Add(ArrayTemplates.CreateLdlen(Type, U1.SmallArrayPermutations, false));
			results.Add(ArrayTemplates.CreateStelem(Type, U1.SmallArrayPermutations, AllPermutations, false));
			results.Add(ArrayTemplates.CreateLdelem(Type, U1.SmallArrayPermutations, AllPermutations, false));
			results.Add(ArrayTemplates.CreateLdelema(Type, U1.SmallArrayPermutations, AllPermutations, false));

			return results;
		}
	}
}
