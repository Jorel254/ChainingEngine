﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using ChainingEngine.Models.Adelante;
using ChainingEngine.ViewModels;
using ChainingEngine.Views;
using ChainingEngine.Views.Atras;
using Kit;
using Kit.Sql.Attributes;
using Kit.Sql.Interfaces;

namespace ChainingEngine.Models.Atras
{
    [Serializable]
    public class Evidencia : IGuid
    {
        public Guid HipotesisGuid { get; set; }
        [Ignore]
        public ObservableCollection<Comportamiento> Comportamientos { get; }
        [PrimaryKey]
        public Guid Guid { get; set; }
        public string Descripcion { get; set; }
        public Evidencia(string descripcion, params Comportamiento[] comportamientos)
        {
            this.Descripcion = descripcion;
            this.Comportamientos = new ObservableCollection<Comportamiento>(comportamientos.Shuffle(new Random()));
        }

        public Evidencia()
        {
            this.Comportamientos = new ObservableCollection<Comportamiento>();
        }
        public async Task<Comportamiento> Run(MainView window)
        {
            var view = new HipotesisView();
            var model = new HipotesisViewModel(this);
            view.DataContext = model;
            window.Content = view;
            await Task.Run(() => model.ResetEvent.WaitOne());
            return model.Comportamiento;
        }
        public void Delete()
        {
            foreach (Comportamiento comportamiento in Comportamientos)
            {
                comportamiento.Delete();
            }
            App.SqLite.Delete(this);
        }
        public void Save(Guid hipotesisGuid)
        {
            HipotesisGuid = hipotesisGuid;
            App.SqLite.InsertOrReplace(this);

            if (!Comportamientos.Any())
            {
                return;
            }

            Conclusion[] conclusiones = Comportamientos.Select(x => x.Conclusion).DistinctBy(x => x.Descripcion).ToArray();
            for (int i = 0; i < conclusiones.Length; i++)
            {
                conclusiones[i].Save();
            }

            for (var index = 0; index < Comportamientos.Count; index++)
            {
                Comportamiento comportamiento = Comportamientos[index];
                comportamiento.Save(Guid,conclusiones[index]);
            }
        }

        public void Load()
        {
            IEnumerable<Comportamiento> comportamientos = App.SqLite.Table<Comportamiento>().Where(x => x.EvidenciaGuid == this.Guid).ToArray();
            for (int i = 0; i < comportamientos.Count(); i++)
            {
                comportamientos.ElementAt(i).Load();
            }
            this.Comportamientos.AddRange(comportamientos);
        }
    }
}
