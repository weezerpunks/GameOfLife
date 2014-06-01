using System;
using System.Threading.Tasks;

namespace Life
{
    public class LifeSimulation
    {
        private bool[,] world;
        private bool[,] nextGeneration;
        private Task processTask;
        public static int[] resultat= new int[100]; 
        public LifeSimulation(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException("Size must be greater than zero");
            this.Size = size;
            world = new bool[size, size];
            nextGeneration = new bool[size, size];
        }

        public int Size { get; private set; }
        public int Generation { get; private set; }

        public Action<bool[,]> NextGenerationCompleted;

        public bool this[int x, int y]
        {
            get { return this.world[x, y]; }
            set { this.world[x, y] = value; }
        }

        public bool ToggleCell(int x, int y)
        {
            bool currentValue = this.world[x, y];
            return this.world[x, y] = !currentValue;
        }

        public void Update()
        {
            if (this.processTask != null && this.processTask.IsCompleted)
            {
                // when a generation has completed
                // now flip the back buffer so we can start processing on the next generation
                var flip = this.nextGeneration;
                this.nextGeneration = this.world;
                this.world = flip;
                Generation++;

                // begin the next generation's processing asynchronously
                this.processTask = this.ProcessGeneration();

                if (NextGenerationCompleted != null) NextGenerationCompleted(this.world);
            }
        }

        public void BeginGeneration()
        {
            if (this.processTask == null || (this.processTask != null && this.processTask.IsCompleted))
            {
                // only begin the generation if the previous process was completed
                this.processTask = this.ProcessGeneration();
            }
        }

        public void Wait()
        {
            if (this.processTask != null)
            {
                this.processTask.Wait();
            }
        }

        public static int nbPattern = 0;

        private Task ProcessGeneration()
        {
            return Task.Factory.StartNew(() =>
            {
                Parallel.For(0, Size, x =>
                {
                    Parallel.For(0, Size, y =>
                    {
                        int numberOfNeighbors = IsNeighborAlive(world, Size, x, y, -1, 0)
                            + IsNeighborAlive(world, Size, x, y, -1, 1)
                            + IsNeighborAlive(world, Size, x, y, 0, 1)
                            + IsNeighborAlive(world, Size, x, y, 1, 1)
                            + IsNeighborAlive(world, Size, x, y, 1, 0)
                            + IsNeighborAlive(world, Size, x, y, 1, -1)
                            + IsNeighborAlive(world, Size, x, y, 0, -1)
                            + IsNeighborAlive(world, Size, x, y, -1, -1);

                        bool shouldLive = false;
                        bool isAlive = world[x, y];


                        //Additionne le nombre de pattern sur cette generation si elle corespond 
                        if(isAlive && numberOfNeighbors == 3)
                        {
                            if(verfierCarree(world,Size,x,y)&&verifierContour(world,Size,x,y))
                            {
                                ecrireValeur(Program.i);
                            }
                        }

                        if (isAlive && (numberOfNeighbors == 2 || numberOfNeighbors == 3))
                        {
                            shouldLive = true;
                        }
                        else if (!isAlive && numberOfNeighbors == 3) // zombification
                        {
                            shouldLive = true;
                        }

                        nextGeneration[x, y] = shouldLive;

                    });
                });
            });
        }
        // augmenter a la position de la generation actuelle le nombre de pattern
        private void ecrireValeur(int gen)
        {
            resultat[gen-1]++;
        } 

        //verifie si le point verifier est un carre
        private static bool verfierCarree(bool[,] world, int Size, int x, int y) 
        {
            int total = IsNeighborAlive(world, Size, x, y, 0, 1)
                + IsNeighborAlive(world, Size, x, y, 1, 1)
                            + IsNeighborAlive(world, Size, x, y, 1, 0);

            return total == 3;
        } 

        //si c'est un carre ( 4 point ensemble ) on verifie si le contour est vide
        private static bool verifierContour(bool [,] world,int Size,int x,int y)
        {
            int total = IsNeighborAlive(world, Size, x, y, -1, -1)
                            + IsNeighborAlive(world, Size, x, y, 0, -1)
                            + IsNeighborAlive(world, Size, x, y, 1, -1)
                            + IsNeighborAlive(world, Size, x, y, 2, -1)
                            + IsNeighborAlive(world, Size, x, y, -1, 0)
                            + IsNeighborAlive(world, Size, x, y, 2, 0)
                            + IsNeighborAlive(world, Size, x, y, -1, 1)
                            + IsNeighborAlive(world, Size, x, y, 2, 1)
                            + IsNeighborAlive(world, Size, x, y, -1, 2)
                            + IsNeighborAlive(world, Size, x, y, 0, 2)
                            + IsNeighborAlive(world, Size, x, y, 1, 2)
                            + IsNeighborAlive(world, Size, x, y, 2, 2);

            return total == 0;
        }

        private static int IsNeighborAlive(bool[,] world, int size, int x, int y, int offsetx, int offsety)
        {
            int result = 0;

            int proposedOffsetX = x + offsetx;
            int proposedOffsetY = y + offsety;
            bool outOfBounds = proposedOffsetX < 0 || proposedOffsetX >= size | proposedOffsetY < 0 || proposedOffsetY >= size;
            if (!outOfBounds)
            {
                result = world[x + offsetx, y + offsety] ? 1 : 0;
            }
            return result;
        }
    }

    class Program
    {
        public static int i = 0;
        static void Main(string[] args)
        {
            LifeSimulation sim = new LifeSimulation(75);

            int nbCells = new Random().Next(100, 500);
            Random Ran = new Random();

            //met des cellule random a on 
            for (int j = 0; j < nbCells; j++){
                sim.ToggleCell(Ran.Next(25, 75), Ran.Next(25, 75));
            }
            
            sim.BeginGeneration();
            sim.Wait();
            ++i;
            
            // on fait 100 generation
            for (int k = 0; k < 100;++k )
            {
                sim.Update();
                sim.Wait();
                OutputBoard(sim, i);
                ++i;
                System.Threading.Thread.Sleep(1); // default 100
                Console.Clear();
            }
            ecrireFichier(LifeSimulation.resultat);
        }


        //ECRIT LES DONNES RECUILLIT DANS UN FICHIER
        private static void ecrireFichier(int[] tab)
        {
           System.IO.StreamWriter fichier =new System.IO.StreamWriter("C:\\www\\Resultmath.txt",true);

           string test = String.Join(";", LifeSimulation.resultat);

           fichier.WriteLine(test);

           fichier.Close();

        }

        //AFFICHE LE TABLEAU DE GAME OF LIFE A LECRAN EN CONSOLE
        private static void OutputBoard(LifeSimulation sim,int i)
        {
            var line = new String('-', sim.Size);

            Console.WriteLine(line);

            Console.WriteLine("Generation : " + i);
            
            for (int y = 0; y < sim.Size; y++)
            {
                for (int x = 0; x < sim.Size; x++)
                {
                    Console.Write(sim[x, y] ? "O" : " ");
                }

                Console.WriteLine();
            }
        }
    }
}
 