using System;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Sketch;
using TrainingDataPreprocessor;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Files;

namespace Congeal
{
    /// <summary>
    /// This class creates some Designations out of labeled data.
    /// 
    /// Takes in a preprocessed TrainingData file as created by TrainingDataPreprocessor
    /// </summary>
    class Trainer
    {
		/// <summary>
		/// Private inner class wrapper around a Thread object for performing computation
		/// </summary>
		private class TrainThread
		{
			#region Internals

			private Thread _thread;
			private TrainingData _td;
			private Queue<string> _gates;

			#endregion

			#region Constructors
			/// <summary>
			/// Create a new TrainThread
			/// </summary>
			/// <param name="td">The TrainingData object</param>
			/// <param name="gates">The gates to operate on</param>
			public TrainThread(TrainingData td, Queue<string> gates)
			{
				_td = td;
				_gates = gates;
			}

			#endregion

			#region Thread Functions

			/// <summary>
			/// Start the thread executing
			/// </summary>
			public void Start()
			{
				_thread = new Thread(new ThreadStart(DoComputation));
				_thread.Start();
			}

			/// <summary>
			/// Wrapper around Thread.Join()
			/// <seealso cref="System.Threading.Thread.Join()"/>
			/// </summary>
			public void Join()
			{
				_thread.Join();
			}

			#endregion

			#region Computation

			private void DoComputation()
			{
				while(_gates.Count > 0)
				{
					string gate = _gates.Dequeue();
					if (_td.Images(gate).Count > 0)
					{
						Console.WriteLine("Training {0}s", gate);
						Designation d = new Designation(_td.Images(gate), gate, 1, _td.CanonicalExample(gate));
						d.train();
						Designation.saveTraining(d, Util.getDir() + @"\output\trained_" + gate + FUtil.Extension(Filetype.CONGEALER_TRAINING_DATA));
					}
					else
					{
						Console.WriteLine("No images found for gate {0}", gate);
					}
				}
			}

			#endregion
		}

        public static void Main(String[] args)
        {
			if (args.Length == 0)
			{
				Console.WriteLine("Please provide the path to a Preprocessed training data file as an argument to this program");
				Environment.Exit(1);
			}
			TrainingData td = TrainingData.ReadFromFile(args[0]);
			int numThreads = Math.Min(Environment.ProcessorCount, td.Gates.Count);
            //numThreads = 1;
            Console.WriteLine("Initializing {0} training threads", numThreads);
			List<Queue<string>> _gateQueues = new List<Queue<string>>(numThreads);
			List<TrainThread> threads = new List<TrainThread>(numThreads);
			for (int i = 0; i < numThreads; ++i)
			{
				_gateQueues.Add(new Queue<string>());
			}
			int which = 0;
			IEnumerator<string> gateEnumerator = td.Gates.GetEnumerator();
			gateEnumerator.MoveNext(); // For some reason, the iterator starts that the -1th index
			for (int gateidx = 0; gateidx < td.Gates.Count; ++gateidx)
			{
				if (gateidx % numThreads == 0)
					which = 0;
				if (gateEnumerator.Current != null)
					_gateQueues[which].Enqueue(gateEnumerator.Current);
				gateEnumerator.MoveNext();
				++which;
			}
			foreach (Queue<string> q in _gateQueues)
			{
				TrainThread t = new TrainThread(td, q);
				threads.Add(t);
			}
			foreach (TrainThread t in threads)
			{
				t.Start();
			}
			foreach (TrainThread t in threads)
			{
				t.Join();
			}
        }

	}
}
