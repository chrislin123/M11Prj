using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M11CCD
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {

            //Random r = new Random();
            //var items = Enumerable.Range(0, 100).Select(x => r.Next(100, 200)).ToList();

            //ParallelQueue(items, DoWork);

            //return;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainFormCCD());
        }

        

        private static void ParallelQueue<T>(List<T> items, Func<T,string> action)
        {
            Queue pending = new Queue(items);
            List<Task> working = new List<Task>();

            while (pending.Count + working.Count != 0)
            {
                if (pending.Count != 0 && working.Count < 16)  // Maximum tasks
                {
                    var item = pending.Dequeue(); // get item from queue
                    working.Add(Task.Run<string>(() => action((T)item))); // run task
                }
                else
                {
                    Task.WaitAny(working.ToArray());
                    List<Task> CompLists = working.Where(x => x.IsCompleted).ToList<Task>();
                    foreach (Task item in CompLists)
                    {
                        var tt = item as Task<string>;
                        Console.WriteLine(tt.Result);
                        
                        //item.r
                    }
                    working.RemoveAll(x => x.IsCompleted); // remove finished tasks
                }
            }
        }

        private static string DoWork(int i) // do your work here.
        {
            // this is just an example
            Task.Delay(i).Wait();
            Console.WriteLine(i);
            return String.Format("Work[{0}]:已完成",i);
        }
    }
}
