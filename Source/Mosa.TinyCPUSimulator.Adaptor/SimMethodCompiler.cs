﻿/*
 * (c) 2013 MOSA - The Managed Operating System Alliance
 *
 * Licensed under the terms of the New BSD License.
 *
 * Authors:
 *  Phil Garcia (tgiphil) <phil@thinkedge.com>
 */

using Mosa.Compiler.Framework;
using Mosa.Compiler.Framework.Stages;
using Mosa.Compiler.MosaTypeSystem;

namespace Mosa.TinyCPUSimulator.Adaptor
{
	internal class SimMethodCompiler : BaseMethodCompiler
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimMethodCompiler" /> class.
		/// </summary>
		/// <param name="compiler">The compiler.</param>
		/// <param name="method">The method.</param>
		/// <param name="simAdapter">The sim adapter.</param>
		/// <param name="basicBlocks">The basic blocks.</param>
		/// <param name="instructionSet">The instruction set.</param>
		public SimMethodCompiler(SimCompiler compiler, MosaMethod method, ISimAdapter simAdapter, BasicBlocks basicBlocks, InstructionSet instructionSet)
			: base(compiler, method, basicBlocks, instructionSet)
		{
			var compilerOptions = Compiler.CompilerOptions;

			// Populate the pipeline
			Pipeline.Add(new IMethodCompilerStage[] {
				new CILDecodingStage(),
				new BasicBlockBuilderStage(),
				new ProtectedRegionStage(),
				new StackSetupStage(),
				new OperandAssignmentStage(),
				new StaticAllocationResolutionStage(),
				new CILTransformationStage(),
				new ConvertCompoundStage(),
				new UnboxValueTypeStage(),

				(compilerOptions.EnablePromoteTemporaryVariablesOptimization) ? new PromoteTempVariablesStage() : null,

				(compilerOptions.EnableSSA) ? new EdgeSplitStage() : null,
				(compilerOptions.EnableSSA) ? new PhiPlacementStage() : null,
				(compilerOptions.EnableSSA) ? new EnterSSAStage() : null,
				(compilerOptions.EnableOptimizations) ? new IROptimizationStage() : null,
				(compilerOptions.EnableSSA) ? new LeaveSSA() : null,

				new ExceptionStage(),

				new PlatformStubStage(),
				new	PlatformEdgeSplitStage(),
				new GreedyRegisterAllocatorStage(),
				new StackLayoutStage(),
				new EmptyBlockRemovalStage(),
				new BlockOrderingStage(),
				new SimCodeGeneratorStage(simAdapter),
				new ProtectedRegionLayoutStage(),
			});
		}
	}
}