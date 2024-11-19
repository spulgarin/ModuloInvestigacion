// Se importan las dependencias necesarias para el funcionamiento del componente
using Microsoft.AspNetCore.Components;  
using Microsoft.JSInterop;             
using System;                          
using System.Collections.Generic;      
using System.Threading.Tasks;          

namespace BlazorFrontEnd.Services
{
   // Esta clase base se utiliza para validar el acceso a las páginas
   // Otros componentes pueden heredar de ella usando @inherits ValidacionAcceso
   public class ValidacionAcceso : ComponentBase
   {
       // Se inyectan las dependencias que el componente necesitará
       // JSRuntime permite la interoperabilidad con JavaScript
       [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
       // NavigationManager maneja la navegación entre páginas
       [Inject] protected NavigationManager Navigation { get; set; } = default!;
       
       // Variables que controlan el estado de la validación
       // yaValidado evita que se realicen validaciones innecesarias
       private bool yaValidado = false;
       // renderizadoPermitido determina si se puede mostrar la página
       private bool renderizadoPermitido = false;

       // Este método se ejecuta después de cada renderizado del componente
       // Es especialmente importante en el primer renderizado
       protected override async Task OnAfterRenderAsync(bool firstRender)
       {
           // Solo se valida en el primer renderizado y si no se ha validado antes
           // Esto previene ciclos infinitos de validación
           if (firstRender && !yaValidado)
           {
               await ValidarAcceso();
           }
           await base.OnAfterRenderAsync(firstRender);
       }

       // Este método controla si el componente debe renderizarse
       protected override bool ShouldRender()
       {
           // Se obtiene la ruta actual limpia (sin base URL y parámetros)
           // Si es la página de login, siempre se permite el renderizado
           if (Navigation.Uri.Replace(Navigation.BaseUri, "/").Split('?')[0].Equals("/login", StringComparison.OrdinalIgnoreCase))
               return true;
           
           // Para otras páginas, el renderizado depende de la validación
           return renderizadoPermitido;
       }

       // Este método realiza la validación principal de acceso
       private async Task ValidarAcceso()
       {
           try
           {
               // Se obtiene el email del usuario desde sessionStorage
               var usuarioEmail = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "usuarioEmail");
               
               // Se obtiene la ruta actual sin base URL ni parámetros
               var rutaActual = Navigation.Uri.Replace(Navigation.BaseUri, "/").Split('?')[0];

               // La página de login siempre es accesible
               if (rutaActual.Equals("/login", StringComparison.OrdinalIgnoreCase))
               {
                   yaValidado = true;
                   renderizadoPermitido = true;
                   return;
               }

               // Si no hay usuario logueado, se redirige al login
               if (string.IsNullOrEmpty(usuarioEmail))
               {
                   await JSRuntime.InvokeVoidAsync("alert", "Sesión no válida. Redirigiendo al login...");
                   Navigation.NavigateTo("/login", true);
                   return;
               }

               // Se obtienen las rutas permitidas desde sessionStorage
               // Se buscan todas las keys que empiezan con 'ruta_'
               var rutas = await JSRuntime.InvokeAsync<List<string>>("eval", @"
                   Object.keys(sessionStorage)
                       .filter(key => key.startsWith('ruta_'))
                       .map(key => sessionStorage.getItem(key))");

               // Si la ruta actual no está permitida, se redirige a inicio
               if (!rutas.Contains(rutaActual))
               {
                   await JSRuntime.InvokeVoidAsync("alert", "No tienes permisos para acceder a esta página");
                   Navigation.NavigateTo("/", true);
                   return;
               }

               // Si todas las validaciones pasan, se permite el acceso
               yaValidado = true;
               renderizadoPermitido = true;
               // Se notifica a Blazor que debe actualizar la UI
               StateHasChanged();
           }
           catch (Exception ex)
           {
               // Si ocurre algún error, se registra y se redirige a inicio
               Console.WriteLine($"Error en validación de acceso: {ex.Message}");
               await JSRuntime.InvokeVoidAsync("alert", "Error en la validación de acceso");
               Navigation.NavigateTo("/", true);
           }
       }
   }
}