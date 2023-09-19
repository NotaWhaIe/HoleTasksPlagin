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
                TaskDialog.Show("Revit", "Связанный файл не найден!");
                return Result.Cancelled;
            }
            linkDoc = revitLinkInstance.First().GetLinkDocument();
            Transform transform = revitLinkInstance.First().GetTotalTransform();
            
            ///Получение стен из связанного файла
            List<Wall> wallsInLinkList = new FilteredElementCollector(linkDoc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .ToList();
            ///Получение перекрытий из связанного файла
            List<Floor> floorsInLinkList = new FilteredElementCollector(linkDoc)
                .OfClass(typeof(Floor))
                .Cast<Floor>()
                .ToList();
            ///Получение трубопроводов
            List<Pipe> pipesList = new FilteredElementCollector(doc)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .ToList();
            ///Получение воздуховодов
            List<Duct> ductsList = new FilteredElementCollector(doc)
                .OfClass(typeof(Duct))
                .Cast<Duct>()
                .ToList();


            ///Обработка геометрии
            ///Обработка геометрии Стен
            ///Обработка геометрии Перекрытий

            return Result.Succeeded;
        }

    }
}
