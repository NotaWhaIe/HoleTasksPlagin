using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace HoleTasksPlagin
{
    [Autodesk.Revit.Attributes.TransactionAttribute(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HoleTasksCommand : IExternalCommand
    {
        static AddInId addinId = new AddInId(new Guid("683709d1-4e06-415a-93d5-81652b0f4c3b"));
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /// Получение текущего документа
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            ///Переменная для связанного файла
            Document linkDoc = null;
            ///Получение доступа к Selection
            Selection sel = commandData.Application.ActiveUIDocument.Selection; ;

            //Выбор связанного файла
            RevitLinkInstanceSelectionFilter selFilterRevitLinkInstance = new RevitLinkInstanceSelectionFilter();
            Reference selRevitLinkInstance = null;
            try
            {
                selRevitLinkInstance = sel.PickObject(ObjectType.Element, selFilterRevitLinkInstance, "Выберите связанный файл!");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            IEnumerable<RevitLinkInstance> revitLinkInstance = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Where(li => li.Id == selRevitLinkInstance.ElementId)
                .Cast<RevitLinkInstance>();
            if (revitLinkInstance.Count() == 0)
            {
                TaskDialog.Show("Ravit", "Связанный файл не найден!");
                return Result.Cancelled;
            }
            linkDoc = revitLinkInstance.First().GetLinkDocument();
            Transform transform = revitLinkInstance.First().GetTotalTransform();

            ///Получение стен из связанного файла
            List<Wall> wallsInLinkList = new FilteredElementCollector(linkDoc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Where(w => w.CurtainGrid == null)
                .ToList();

            //Получение трубопроводов и воздуховодов
            List<Pipe> pipesList = new List<Pipe>();
            List<Duct> ductsList = new List<Duct>();
            //Выбор трубы или воздуховода
            PipeDuctSelectionFilter pipeDuctSelectionFilter = new PipeDuctSelectionFilter();
            IList<Reference> pipeDuctRefList = null;
            try
            {
                pipeDuctRefList = sel.PickObjects(ObjectType.Element, pipeDuctSelectionFilter, "Выберите трубу или воздуховод!");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            foreach (Reference refElem in pipeDuctRefList)
            {
                if (doc.GetElement(refElem).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_PipeCurves))
                {
                    pipesList.Add((doc.GetElement(refElem) as Pipe));
                }
                else if (doc.GetElement(refElem).Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_DuctCurves))
                {
                    ductsList.Add((doc.GetElement(refElem)) as Duct);
                }
            }

            ///Обработка стен








            ElementId selFiId = null;

            ///Разместить семейство в местах пересечений
            if (sel.GetElementIds().Count != 1)
            {
                Reference r = sel.PickObject(ObjectType.Element, "Выберите семейство");
                selFiId = r.ElementId;
            }
            else
            {
                selFiId = sel.GetElementIds().First();
            }

            FamilyInstance selFi = doc.GetElement(selFiId) as FamilyInstance;
            if (selFi == null)
            {
                TaskDialog.Show("Ошибка", "Выбрано не семейство!");
                return Result.Failed;
            }

            FamilySymbol mySymbol = selFi.Symbol;
            View curView = doc.ActiveView;
            Level lv = curView.GenLevel;
            if (lv == null) throw new Exception("Нe удалось получить уровень по его ID");


            List<Grid> grids = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Размещение семейств");
                foreach (Grid g in grids)
                {
                    XYZ p0 = g.Curve.GetEndPoint(0);
                    XYZ p1 = g.Curve.GetEndPoint(1);
                    XYZ p = (p0 + p1) / 2;
                    FamilyInstance fi = doc.Create.NewFamilyInstance(p, mySymbol, lv, StructuralType.NonStructural);
                }
                t.Commit();
            }




            return Result.Succeeded;
        }

    }
}
