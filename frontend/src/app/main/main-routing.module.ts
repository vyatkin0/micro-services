import {RouterModule, Routes} from '@angular/router';

import {MainComponent} from './main.component';
import { NgModule } from '@angular/core';
import {OrdersComponent} from './orders/orders.component';
import {PageNotFoundComponent} from '../page-not-found/page-not-found.component';
import {RolesComponent} from './roles/roles.component';
import {StartComponent} from './start/start.component';
import {UsersComponent} from './users/users.component';

const routes: Routes = [
  {
      path:"",
      component: MainComponent,
      children: [
        {
          path: "",
          pathMatch: "full",
          redirectTo: "start"
        },
        {
          path: "start",
          pathMatch: "full",
          component: StartComponent
        },
        {
          path: "users",
          pathMatch: "full",
          component: UsersComponent
        },
        {
          path: "roles",
          pathMatch: "full",
          component: RolesComponent
        },
        {
          path: "orders",
          pathMatch: "full",
          component: OrdersComponent
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
export class AppRoutingModule { }
