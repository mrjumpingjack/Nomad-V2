using GMap.NET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ControllPanel
{
    public class DataHelper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler CurrentVehiclePosChanged;
        public event EventHandler CurrentVehicleHeadingChnaged;




        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private PointLatLng target;
        public PointLatLng Target
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
                OnPropertyChanged();
            }
        }


        private PointLatLng currentCarPos;
        public PointLatLng CurrentCarPos
        {
            get
            {
                return currentCarPos;
            }
            set
            {
                currentCarPos = value;
                OnPropertyChanged();
                CurrentVehiclePosChanged?.Invoke(this, null);
            }
        }

        private double currentCarHeading;
        public double CurrentCarHeading
        {
            get
            {
                return currentCarHeading;
            }
            set
            {
                currentCarHeading = value;
                OnPropertyChanged();
                CurrentVehicleHeadingChnaged?.Invoke(this, null);
            }
        }


        private int[] distances;
        public int[] Distances
        {
            get
            {
                return distances;
            }
            set
            {
                distances = value;
                OnPropertyChanged();

                ValueFront = distances[0];
                ValueBack = distances[1];
                ValueLeft = distances[2];
                ValueRight = distances[3];
                ValueFrontGab = distances[4];
            }
        }

        private int valueFront;
        public int ValueFront
        {
            get
            {
                return valueFront;
            }
            set
            {
                valueFront = value;
                OnPropertyChanged();
            }
        }

        private int valueFrontGab;

        public int ValueFrontGab
        {
            get
            {
                return valueFrontGab;
            }
            set
            {
                valueFrontGab = value;
                OnPropertyChanged();
            }
        }

        private int valueBack;

        public int ValueBack
        {
            get
            {
                return valueBack;
            }
            set
            {
                valueBack = value;
                OnPropertyChanged();
            }
        }

        private int valueRight;

        public int ValueRight
        {
            get
            {
                return valueRight;
            }
            set
            {
                valueRight = value;
                OnPropertyChanged();
            }
        }

        private int valueLeft;

        public int ValueLeft
        {
            get
            {
                return valueLeft;
            }
            set
            {
                valueLeft = value;
                OnPropertyChanged();
            }
        }

        private bool goToCarPosOnUpdate= true;
        public bool GoToCarPosOnUpdate
        {
            get
            {
                return goToCarPosOnUpdate;
            }

            set
            {
                goToCarPosOnUpdate = value;
                OnPropertyChanged();
            }
        }

        private int rotations;

        public int Rotations
        {
            get
            {
                return rotations;
            }
            set
            {
                rotations = value;
                double UmfangInM = 2 * Math.PI * 0.05;
                double SpeedinMpSek = (UmfangInM * (rotations));
                SpeedinKMh = (SpeedinMpSek/1000) / (1f / 3600f);
                OnPropertyChanged();
            }
        }

        private double speedinKMh;

        public double SpeedinKMh
        {
            get
            {
                return speedinKMh;
            }

            set
            {
                speedinKMh = value;
                OnPropertyChanged();
            }
        }
    }
}
