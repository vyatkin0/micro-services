import { NgModule } from '@angular/core';
import { Routes, RouterModule, UrlSerializer } from '@angular/router';

const routes: Routes = [
  {
    path: "main",
    loadChildren: () => import('./main/main.module')
      .then(m => m.MainModule),
    data: {
      preload: false
    },
  },
  {
    path: "",
    loadChildren: () => import('./anonymous/anonymous.module')
      .then(m => m.AnonymousModule),
    data: {
      preload: false
    }
  }
];

@NgModule({

  imports: [RouterModule.forRoot(
    routes, {
    malformedUriErrorHandler:
      (error: URIError, urlSerializer: UrlSerializer, url: string) =>
        urlSerializer.parse("/page-not-found")
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
