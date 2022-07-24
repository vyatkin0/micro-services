import {FormsModule, ReactiveFormsModule} from '@angular/forms';

import { AnonymousComponent } from './anonymous.component';
import { AnonymousRoutingModule } from './anonymous-routing.module';
import { CommonModule } from '@angular/common';
import { LoginComponent } from "./login/login.component";
import { MaterialModule } from '../../material.module';
import { NgModule } from '@angular/core';
import { RegisterComponent } from "./register/register.component";
import { SpinnerOverlayService } from '../services/spinner';

@NgModule({
  declarations: [
    AnonymousComponent,
    LoginComponent,
    RegisterComponent
  ],
  imports: [
    CommonModule,
    AnonymousRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    MaterialModule
  ],
  providers: [SpinnerOverlayService],
})
export class AnonymousModule { }
