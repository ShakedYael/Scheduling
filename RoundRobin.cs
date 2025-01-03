using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class RoundRobin : SchedulingPolicy
    {
        private Queue<int> processQueue;
        private int quantum; 
        public RoundRobin(int iQuantum)
        {
            processQueue = new Queue<int>();
            quantum = iQuantum;
        }

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            // Check if there are processes in the queue
            if (processQueue.Count > 0)
            {
                
                for (int i = 0; i < processQueue.Count; i++)
                {
                    int nextProcessId = processQueue.Dequeue(); // Get the next process in the queue
                    ProcessTableEntry nextProcess = dProcessTable[nextProcessId];

                    // Check if the next process is ready to run (not blocked or terminated)
                    if (!nextProcess.Blocked && !nextProcess.Done)
                    {
                        nextProcess.Quantum = quantum;
                        processQueue.Enqueue(nextProcessId);
                        return nextProcessId; // Return the next process to be executed
                    }
                    else
                    {
                        processQueue.Enqueue(nextProcessId);
                    }
                }
            }

            return -1; // No process available to execute
        }

        public override void AddProcess(int iProcessId)
        {
            processQueue.Enqueue(iProcessId); 

        }

        public override bool RescheduleAfterInterrupt()
        {
            return true;
        }
    }
}
