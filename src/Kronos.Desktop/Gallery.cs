// Copyright 2026 JLC-DevLab. Volatility Software License 1.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Kronos.Core;
namespace Kronos.Desktop;
public sealed partial class MainWindow
{
    private async Task OpenGallery()
    {
        var c=RequireCase();
        if(forensicRuns.SelectedItem is not ForensicAnalysis run)throw new InvalidOperationException("Selecciona un análisis guardado.");
        status.Text="Preparando galería: reconociendo imágenes y comprobando el manifiesto…";
        var photos=await c.LoadPhotosAsync(run);
        var tiles=new WrapPanel{Orientation=Orientation.Horizontal};
        var caption=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var loadedBitmaps=new List<Bitmap>();
        var page=0;var generation=0;
        var dialog=new Window{Title="Fotos recuperadas · KRONOS",Width=1080,Height=760,MinWidth=650,MinHeight=450,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var previous=new Button{Content="← Anteriores"};var next=new Button{Content="Siguientes →"};
        async Task RenderPage()
        {
            var currentGeneration=++generation;
            tiles.Children.Clear();foreach(var bitmap in loadedBitmaps)bitmap.Dispose();loadedBitmaps.Clear();
            caption.Text=$"{photos.Count:N0} imágenes únicas por SHA-256 · página {page+1} · selecciona una para ampliarla. Las imágenes no compatibles muestran una incidencia.";
            previous.IsEnabled=page>0;next.IsEnabled=(page+1)*24<photos.Count;
            foreach(var photo in photos.Skip(page*24).Take(24))
            {
                try
                {
                    var bytes=await c.LoadPhotoBytesAsync(run,photo);
                    if(currentGeneration!=generation)return;
                    var size=CaseService.ReadPhotoDimensions(bytes);
                    using var stream=new MemoryStream(bytes);var thumbnail=size.Width>=size.Height?Bitmap.DecodeToWidth(stream,Math.Min(220,size.Width)):Bitmap.DecodeToHeight(stream,Math.Min(155,size.Height));
                    loadedBitmaps.Add(thumbnail);
                    var image=new Image{Source=thumbnail,Width=220,Height=155,Stretch=Stretch.Uniform};
                    var button=new Button{Margin=new Thickness(6),Content=new StackPanel{Spacing=5,Children={image,new TextBlock{Text=Path.GetFileName(photo.File),MaxWidth=220,TextTrimming=TextTrimming.CharacterEllipsis}}}};
                    button.Click+=async(_,_)=>
                    {
                        try
                        {
                        var fullBytes=await c.LoadPhotoBytesAsync(run,photo);
                        var dimensions=CaseService.ReadPhotoDimensions(fullBytes);
                        using var fullStream=new MemoryStream(fullBytes);using var bitmap=dimensions.Width>=dimensions.Height?Bitmap.DecodeToWidth(fullStream,Math.Min(1800,dimensions.Width)):Bitmap.DecodeToHeight(fullStream,Math.Min(1400,dimensions.Height));
                        var note=new Button{Content="Añadir esta foto a las notas"};
                        note.Click+=(_,_)=>notes.Text+=$"\n\nImagen · evidencia {run.EvidenceId}\nFuente: {run.RelativeDirectory}/{photo.File}\nSHA-256: {photo.Sha256}\nInterpretación del analista: ";
                        var content=new Grid{RowDefinitions=new RowDefinitions("*,Auto,Auto"),Margin=new Thickness(16)};
                        content.Children.Add(new Image{Source=bitmap,Stretch=Stretch.Uniform});
                        var source=new TextBlock{Text=$"{photo.File}\nSHA-256: {photo.Sha256}",TextWrapping=TextWrapping.Wrap};Grid.SetRow(source,1);content.Children.Add(source);Grid.SetRow(note,2);content.Children.Add(note);
                        await new Window{Title="Detalle de imagen",Width=1000,Height=750,Content=content}.ShowDialog(dialog);
                        }
                        catch(Exception ex){caption.Text="No se puede abrir la imagen: "+ex.Message;}
                    };
                    tiles.Children.Add(button);
                }
                catch(Exception ex){tiles.Children.Add(new TextBlock{Text="No se puede mostrar "+Path.GetFileName(photo.File)+": "+ex.Message,Width=220,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)});}
            }
        }
        previous.Click+=async(_,_)=>{page--;await RenderPage();};next.Click+=async(_,_)=>{page++;await RenderPage();};
        var toolbar=new StackPanel{Spacing=8,Children={caption,new WrapPanel{Children={previous,next}}}};
        var layout=new Grid{RowDefinitions=new RowDefinitions("Auto,*"),Margin=new Thickness(16)};
        layout.Children.Add(toolbar);var scroll=new ScrollViewer{Content=tiles};Grid.SetRow(scroll,1);layout.Children.Add(scroll);dialog.Content=layout;
        dialog.Closed+=(_,_)=>{generation++;foreach(var bitmap in loadedBitmaps)bitmap.Dispose();};
        var showing=dialog.ShowDialog(this);await RenderPage();await showing;
    }
}
