import { DialogAddOrder, DialogAddRole, DialogAddUser, DialogConfirm, DialogUser, DialogUserRoles } from "./dialogs/dialog.components";
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './main-routing.module';
import { CommonModule } from '@angular/common';
import { DialogChangePassword } from "./dialogs/dialog-change-password";
import { MainComponent } from './main.component';
import { MaterialModule } from '../../material.module';
import { NgModule } from '@angular/core';
import { OrdersComponent } from "./orders/orders.component";
import { RolesComponent } from "./roles/roles.component";
import { SpinnerOverlayService } from '../services/spinner';
import { StartComponent } from "./start/start.component";
import { UsersComponent } from "./users/users.component";

@NgModule({
  declarations: [
    MainComponent,
    DialogUser,
    DialogUserRoles,
    DialogConfirm,
    DialogAddUser,
    DialogAddRole,
    DialogAddOrder,
    DialogChangePassword,
    StartComponent,
    UsersComponent,
    RolesComponent,
    OrdersComponent
  ],
  imports: [
    CommonModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  providers: [SpinnerOverlayService],
})
export class MainModule { }
