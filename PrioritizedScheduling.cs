using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class PrioritizedScheduling : SchedulingPolicy
    {
        private Queue<int> processQueue;
        private int quantum;
        public PrioritizedScheduling(int iQuantum)
        {
            quantum = iQuantum;
            processQueue = new Queue<int>();
        }

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            // Check if there are processes in the queue
            if (processQueue.Count > 0)
            {
                int maxPriority = -1;
                int nextProcess = 0 ;
                for (int i = 0; i < processQueue.Count; i++)
                {
                    int currentProcess = processQueue.Peek(); // Get the next process in the queue
                    ProcessTableEntry curProcessTable = dProcessTable[currentProcess];
                    int priority = curProcessTable.Priority;
                    if (priority > maxPriority && !curProcessTable.Blocked && !curProcessTable.Done)  // Check if the next process is ready to run (not blocked or terminated)

                    {
                        maxPriority = priority;
                        nextProcess = currentProcess;
                    }
                    processQueue.Enqueue(processQueue.Dequeue()); //put the first process at the end

                }
                for(int i = 0; i < processQueue.Count; i++)
                {
                    int currentProcess = processQueue.Peek(); // Get the next process in the queue

                    if (currentProcess == nextProcess)
                    {
                        processQueue.Enqueue(processQueue.Dequeue()); //put the first process at the end
                        return currentProcess;
                    }

                    processQueue.Enqueue(processQueue.Dequeue()); //put the first process at the end

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
