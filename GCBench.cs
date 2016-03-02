// This is adapted from a benchmark written by John Ellis and Pete Kovac
// of Post Communications.
// It was modified by Hans Boehm of Silicon Graphics.
// It was ported to C# by Daniel Spoonhower of Carnegie Mellon University.
// The following comments appeared in Hans's version.
//
// 	This is no substitute for real applications.  No actual application
//	is likely to behave in exactly this way.  However, this benchmark was
//	designed to be more representative of real applications than other
//	Java GC benchmarks of which we are aware.
//	It attempts to model those properties of allocation requests that
//	are important to current GC techniques.
//	It is designed to be used either to obtain a single overall performance
//	number, or to give a more detailed estimate of how collector
//	performance varies with object lifetimes.  It Writes the time
//	required to allocate and collect balanced binary trees of various
//	sizes.  Smaller trees result in shorter object lifetimes.  Each cycle
//	allocates roughly the same amount of memory.
//	Two data structures are kept around during the entire process, so
//	that the measured performance is representative of applications
//	that maintain some live in-memory data.  One of these is a tree
//	containing many pointers.  The other is a large array containing
//	double precision floating point numbers.  Both should be of comparable
//	size.
//
//	The results are only really meaningful together with a specification
//	of how much memory was used.  It is possible to trade memory for
//	better time performance.  This benchmark should be run in a 32 MB
//	heap, though we don't currently know how to enforce that uniformly.
//
//	Unlike the original Ellis and Kovac benchmark, we do not attempt
// 	measure pause times.  This facility should eventually be added back
//	in.  There are several reasons for omitting it for now.  The original
//	implementation depended on assumptions about the thRead scheduler
//	that don't hold uniformly.  The results really measure both the
//	scheduler and GC.  Pause time measurements tend to not fit well with
//	current benchmark suites.  As far as we know, none of the current
//	commercial Java implementations seriously attempt to minimize GC pause
//	times.

using System;
using System.Runtime;

class Node {
  public Node left, right;
  public int i = 0, j = 0;
  public Node(Node l, Node r) { left = l; right = r; }
  public Node() { }
}

public class GCBench {

	public static int kStretchTreeDepth    = 18;	// about 16Mb
	public static int kLongLivedTreeDepth  = 16;  // about 4Mb
	public static int kArraySize  = 500000;  // about 4Mb
	public static int kMinTreeDepth = 4;
	public static int kMaxTreeDepth = 16;

	private const bool PRINT = true;

	// Nodes used by a tree of a given size
	static int TreeSize(int i) {
	    	return ((1 << (i + 1)) - 1);
	}

	// Number of iterations to use for a given tree depth
	static int NumIters(int i) {
                return 2 * TreeSize(kStretchTreeDepth) / TreeSize(i);
        }

	// Build tree top down, assigning to older objects. 
	static void Populate(int iDepth, Node thisNode) {
		if (iDepth<=0) {
			return;
		} else {
			iDepth--;
			thisNode.left  = new Node();
			thisNode.right = new Node();
			Populate (iDepth, thisNode.left);
			Populate (iDepth, thisNode.right);
		}
	}

	// Build tree bottom-up
	static Node MakeTree(int iDepth) {
		if (iDepth<=0) {
			return new Node();
		} else {
			return new Node(MakeTree(iDepth-1),
					MakeTree(iDepth-1));
		}
	}

	static void PrintDiagnostics() {
		if (PRINT) {
                    long bTotalMemory = GC.GetTotalMemory(false);
                    long lTotalMemory = GC.GetTotalMemory(true);
                        
                    Console.WriteLine(" Total memory available="+bTotalMemory +"->"+ lTotalMemory + " bytes");
                    GC.CollectionCount(0);
		}
	}

	static void TimeConstruction(int depth) {
		long    tStart, tFinish;
		int 	iNumIters = NumIters(depth);
		Node	tempTree;

		if (PRINT) {
			Console.WriteLine("Creating " + iNumIters +
														" trees of depth " + depth);
		}
		tStart = Environment.TickCount;
		for (int i = 0; i < iNumIters; ++i) {
			tempTree = new Node();
			Populate(depth, tempTree);
			tempTree = null;
		}
		tFinish = Environment.TickCount;
		if (PRINT) {
			Console.WriteLine("\tTop down construction took "
														+ (tFinish - tStart) + "msecs");
		}
		tStart = Environment.TickCount;
                for (int i = 0; i < iNumIters; ++i) {
                        tempTree = MakeTree(depth);
                        tempTree = null;
                }
                tFinish = Environment.TickCount;
								if (PRINT) {
                Console.WriteLine("\tBottom up construction took "
																			+ (tFinish - tStart) + "msecs");
								}
		
	}

	public static void Main(string[] args) {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
            Console.WriteLine("Latenctmode: "+GCSettings.LatencyMode);

            if (args.Length == 5) {
                kStretchTreeDepth = int.Parse(args[0]);//   = 18;	// about 16Mb
                kLongLivedTreeDepth = int.Parse(args[1]);// = 16;  // about 4Mb
                kArraySize  = int.Parse(args[2]);// 500000;  // about 4Mb
                kMinTreeDepth = int.Parse(args[3]);//4;
                kMaxTreeDepth = int.Parse(args[4]);//16;
            }

            
		Node	longLivedTree;
		Node	tempTree;
		long	tStart, tFinish;
		long	tElapsed;


		if (PRINT) {
			Console.WriteLine(
														" Stretching memory with a binary tree of depth "
														+ kStretchTreeDepth);
		}

		tStart = Environment.TickCount;

                //Console.WriteLine("Total Mem: before strechtredepth"+GC.GetTotalMemory(false));
		// Stretch the memory space quickly
		tempTree = MakeTree(kStretchTreeDepth);
                
		tempTree = null;
		PrintDiagnostics();
		// Create a long lived objects
		if (PRINT) {
			Console.WriteLine(
														" Creating a long-lived binary tree of depth " +
														kLongLivedTreeDepth);
		}
                
		longLivedTree = new Node();
		Populate(kLongLivedTreeDepth, longLivedTree);
                
		// Create long-lived array, filling half of it
		if (PRINT) {
			Console.WriteLine(
														" Creating a long-lived array of "
														+ kArraySize + " doubles");
		}
                
		double[] array = new double[kArraySize];
		for (int i = 0; i < kArraySize/2; ++i) {
			array[i] = 1.0/i;
		}
                
		PrintDiagnostics();

		for (int d = kMinTreeDepth; d <= kMaxTreeDepth; d += 2) {
			TimeConstruction(d);
		}
                PrintDiagnostics();
                
		if (longLivedTree == null || array[1000] != 1.0/1000) {
			if (PRINT) {
				Console.WriteLine("Failed");
				// fake reference to LongLivedTree
				// and array
				// to keep them from being optimized away
			}
		}

		tFinish = Environment.TickCount;
		tElapsed = tFinish-tStart;
		
		if (PRINT) {
			Console.WriteLine("Completed in " + tElapsed + "ms.");
		}
                longLivedTree = null;
                array = null;
                PrintDiagnostics();
                //GC.CollectionCount(0);
	}
} // class JavaGC
