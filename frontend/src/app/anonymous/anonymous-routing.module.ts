import { NgModule } from '@angular/core';
import {Routes, RouterModule} from '@angular/router';

import {AnonymousComponent} from './anonymous.component';
import {LoginComponent} from './login/login.component';
import {RegisterComponent} from './register/register.component';
import {PageNotFoundComponent} from '../page-not-found/page-not-found.component';

const routes: Routes = [
  {
      path:"",
      component: AnonymousComponent,
      children: [
        {
          path: "",
          redirectTo: "login",
          pathMatch: "full"
        },
        {
          path: "login",
          pathMatch: "full",
          component: LoginComponent,
        },
        {
            path: "signup",
            pathMatch: "full",
            component: RegisterComponent
        },
        {
            path: "**",
            component: PageNotFoundComponent
        }
      ],
  }
];

@NgModule({ 
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AnonymousRoutingModule { }
