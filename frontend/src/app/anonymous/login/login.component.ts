import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { rpc, setLoginInfo } from '../../../utils';

import { Router } from '@angular/router';
import { SpinnerOverlayService } from '../../services/spinner';

@Component({
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  form: FormGroup;
  error: string|undefined;

  constructor(
    private spinner: SpinnerOverlayService,
    private fb: FormBuilder,
    private router: Router) {

    this.form = fb.group({
      name: ['', [Validators.required]],
      password: ['', [Validators.required]]
    });

  }

  async login() {
    try {
      this.error = undefined;

      this.spinner.show('Signing in');

      const result = await rpc('identity','accounts','Login', this.form.value, false);

      setLoginInfo(result);

      window.location.href = '/main';
    }
    catch (e: any) {
      this.error = e;
    }
    this.spinner.hide();
  }

  getErrorMessage(control: string) {
    const ctrl = this.form.controls[control];

    if (ctrl.hasError('required')) {
      return 'You must enter a value';
    }

    return 'Wrong value';
  }
}
