using System;
using System.Runtime.InteropServices;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// Helper class to allow time measurements with high-resolution timers
	/// </summary>
	public class PerfTiming
	{
		/// <summary>
		/// Win32 API function to retrieve a high-resolution performance counter
		/// value
		/// </summary>
		/// <param name="nPfCt">The performance counter value retrieved</param>
		/// <returns>Returns true on success</returns>

		[DllImport("KERNEL32")]

		public static extern bool QueryPerformanceCounter(ref Int64 nPfCt);

		/// <summary>
		/// Win32 API function to get the performance frequency which must be
		/// used 
		/// </summary>
		/// <param name="nPfFreq">The performance frequency</param>
		/// <returns>Returns true on success</returns>

		[DllImport("KERNEL32")]

		public static extern bool QueryPerformanceFrequency(ref Int64 nPfFreq);

		/// <summary>The machine's performance frequency.</summary>
		protected Int64 m_i64Frequency;
		/// <summary>The start point of our performance measurement.</summary>
		protected Int64 m_i64Start;

		/// <summary>
		/// Create a new instance for performance measurements
		/// and initialize it with the system's performance frequency
		/// </summary>
		public PerfTiming()
		{
			QueryPerformanceFrequency(ref m_i64Frequency);
			m_i64Start = 0;
		}

		/// <summary>
		/// Start performance measurement by retrieving a first performance counter value
		/// </summary>
		public void Start()
		{
			QueryPerformanceCounter(ref m_i64Start);
		}

		/// <summary>
		/// Stop performance measurement. Calculate the time passed since Start() and
		/// return the calculated time.
		/// </summary>
		/// <returns>The time passed between the last call to Start and the call to End in seconds.</returns>
		public double End()
		{
			Int64 i64End = 0;
			QueryPerformanceCounter(ref i64End);
			return ((i64End - m_i64Start) / (double)m_i64Frequency);
		}
	}
}