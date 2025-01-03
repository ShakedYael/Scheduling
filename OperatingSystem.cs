using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class OperatingSystem
    {
        public Disk Disk { get; private set; }
        public CPU CPU { get; private set; }
        private Dictionary<int, ProcessTableEntry> m_dProcessTable;
        private List<ReadTokenRequest> m_lReadRequests;
        private int m_cProcesses;
        private SchedulingPolicy m_spPolicy;
        private static int IDLE_PROCESS_ID = 0;

        public OperatingSystem(CPU cpu, Disk disk, SchedulingPolicy sp)
        {
            CPU = cpu;
            Disk = disk;
            m_dProcessTable = new Dictionary<int, ProcessTableEntry>();
            m_lReadRequests = new List<ReadTokenRequest>();
            cpu.OperatingSystem = this;
            disk.OperatingSystem = this;
            m_spPolicy = sp;

            //create an "idle" process here
            IdleCode idleCode = new IdleCode();
            m_dProcessTable[IDLE_PROCESS_ID] = new ProcessTableEntry(IDLE_PROCESS_ID,"Idle Process", idleCode);
            m_dProcessTable[IDLE_PROCESS_ID].StartTime = CPU.TickCount;
            m_dProcessTable[IDLE_PROCESS_ID].Priority = -1;
            m_spPolicy.AddProcess(IDLE_PROCESS_ID);
            m_cProcesses++;

        }

        public void CreateProcess(string sCodeFileName)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }
        public void CreateProcess(string sCodeFileName, int iPriority)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].Priority = iPriority;
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }

        public void ProcessTerminated(Exception e)
        {
            if (e != null)
                Console.WriteLine("Process " + CPU.ActiveProcess + " terminated unexpectedly. " + e);
            m_dProcessTable[CPU.ActiveProcess].Done = true;
            m_dProcessTable[CPU.ActiveProcess].Console.Close();
            m_dProcessTable[CPU.ActiveProcess].EndTime = CPU.TickCount;
            ActivateScheduler();
        }

        public void TimeoutReached()
        {
            ActivateScheduler();
        }

        public void ReadToken(string sFileName, int iTokenNumber, int iProcessId, string sParameterName)
        {
            ReadTokenRequest request = new ReadTokenRequest();
            request.ProcessId = iProcessId;
            request.TokenNumber = iTokenNumber;
            request.TargetVariable = sParameterName;
            request.Token = null;
            request.FileName = sFileName;
            m_dProcessTable[iProcessId].Blocked = true;
            if (Disk.ActiveRequest == null)
                Disk.ActiveRequest = request;
            else
                m_lReadRequests.Add(request);
            CPU.ProgramCounter = CPU.ProgramCounter + 1;
            ActivateScheduler();
        }

        public void Interrupt(ReadTokenRequest rFinishedRequest)
        {
            //implement an "end read request" interrupt handler.
            //translate the returned token into a value (double). 
            //when the token is null, EOF has been reached.
            //write the value to the appropriate address space of the calling process.
            //activate the next request in queue on the disk.
            double tokenValue = double.NaN;  

            if(rFinishedRequest.Token != null) // check if the token is not null and can be parsed into a double value
            {
                double.TryParse(rFinishedRequest.Token, out tokenValue);

            }
           
                // Write the parsed token value to the appropriate address space of the calling process
                var process = m_dProcessTable[rFinishedRequest.ProcessId];
                process.Blocked = false;
                process.AddressSpace[rFinishedRequest.TargetVariable] = tokenValue;
           
            //next request in queue on the disk
            ActivateNextRequestOnDisk();

            if (m_spPolicy.RescheduleAfterInterrupt())
                ActivateScheduler();
        }
        private void ActivateNextRequestOnDisk()
        {
            // Check if there are read requests in the queue
            if (m_lReadRequests.Count > 0)
            {
                ReadTokenRequest nextRequest = m_lReadRequests[0];
                m_lReadRequests.RemoveAt(0);  
                Disk.ActiveRequest = nextRequest;

            }
        }
        

        private ProcessTableEntry ContextSwitch(int iEnteringProcessId)
        {
            //your code here
            //implement a context switch, switching between the currently active process on the CPU to the process with pid iEnteringProcessId
            //You need to switch the following: ActiveProcess, ActiveAddressSpace, ActiveConsole, ProgramCounter.
            //All values are stored in the process table (m_dProcessTable)
            //Our CPU does not have registers, so we do not store or switch register values.
            //returns the process table information of the outgoing process
            //After this method terminates, the execution continues with the new process

            //update starvation for each process:
            foreach (var processEntry in m_dProcessTable.Values)
            {
                // check if the process is ready -not blocked and not done
                if (!processEntry.Blocked && !processEntry.Done)
                {
                    //starvation time for the process (current tick - last CPU time)
                    int starvationTime = CPU.TickCount - processEntry.LastCPUTime;

                    // update max
                    if (starvationTime > processEntry.MaxStarvation)
                    {
                        processEntry.MaxStarvation = starvationTime;
                    }
                }
            }

            if (CPU.ActiveProcess != iEnteringProcessId)
            {
                if (CPU.ActiveProcess != -1)
                {
                    // Save the context of the current active process
                    m_dProcessTable[CPU.ActiveProcess].ProgramCounter = CPU.ProgramCounter;
                    m_dProcessTable[CPU.ActiveProcess].LastCPUTime = CPU.TickCount;

                }

                if (CPU.ActiveAddressSpace == null || CPU.ActiveAddressSpace.ProcessId != iEnteringProcessId)
                {
                    CPU.ActiveAddressSpace = m_dProcessTable[iEnteringProcessId].AddressSpace; 
                    CPU.ActiveConsole = m_dProcessTable[iEnteringProcessId].Console; 
                                                        
                }

                // Activate new process
                if (m_dProcessTable[iEnteringProcessId].Quantum != 0) //for RoundRobin
                {

                    CPU.RemainingTime = m_dProcessTable[iEnteringProcessId].Quantum;
                }

                CPU.ActiveProcess = iEnteringProcessId;
                CPU.ProgramCounter = m_dProcessTable[iEnteringProcessId].ProgramCounter;

                return m_dProcessTable[CPU.ActiveProcess];
            }
            else
            {
                // No need for a context switch, return the current active process
                return m_dProcessTable[CPU.ActiveProcess];
            }
        }

        public void ActivateScheduler()
        {
            int iNextProcessId = m_spPolicy.NextProcess(m_dProcessTable);
            if (iNextProcessId == -1)
            {
                Console.WriteLine("All processes terminated or blocked.");
                CPU.Done = true;
            }

            else
            {
                bool bOnlyIdleRemains = false;

                bOnlyIdleRemains = m_dProcessTable.Values.All(process => process.ProcessId == IDLE_PROCESS_ID || process.Done);


                if (bOnlyIdleRemains)
                {
                    Console.WriteLine("Only idle remains.");
                    CPU.Done = true;
                }
                else
                    ContextSwitch(iNextProcessId);
            }
        }

        public double AverageTurnaround()
        {

            int completedProcessesCount = 0;
            int totalTurnaroundTime = 0;

            foreach (var processEntry in m_dProcessTable.Values)
            {
                if (processEntry.Done)
                {
                    // turnaround time for the completed process
                    int turnaroundTime = processEntry.EndTime - processEntry.StartTime;
                    totalTurnaroundTime += turnaroundTime;
                    completedProcessesCount++;
                }
            }

            //division by zero
            if (completedProcessesCount == 0)
            {
                throw new InvalidOperationException("No completed processes found.");
            }

            // calculate avg
            double averageTurnaround = (double)totalTurnaroundTime / completedProcessesCount;
            return averageTurnaround;
        }
        public int MaximalStarvation()  
        {
            //Compute the maximal time that some project has waited in a ready stage without receiving CPU time.
            int maximalStarvationTime = 0;

            foreach (var processEntry in m_dProcessTable.Values)
            {
                
                    // update max
                    if (maximalStarvationTime < processEntry.MaxStarvation)
                    {
                         maximalStarvationTime =processEntry.MaxStarvation;
                    }
                
            }

            return maximalStarvationTime;
        }
    }
}
