import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { rpc, setLoginInfo } from '../../../utils';

import { Router } from '@angular/router';
import { SpinnerOverlayService } from '../../services/spinner';

@Component({
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {

  form: FormGroup;
  error: string | undefined;

  constructor(
    private spinner: SpinnerOverlayService,
    fb: FormBuilder,
    private router: Router) {

    this.form = fb.group({
      name: ['', [Validators.minLength(8), Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', [Validators.maxLength(300), Validators.required]],
      lastName: ['', [Validators.maxLength(300), Validators.required]],
      company: ['', [Validators.maxLength(300), Validators.required]],
      password: ['', [Validators.minLength(8), Validators.maxLength(300), Validators.required]],
      confirmPassword: [''],
    }, { validators: [this.matchPasswordsValidator()] });
  }

  matchPasswordsValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (this.form?.value.confirmPassword !== this.form?.value.password) {
        this.form.controls['confirmPassword'].setErrors({ matchPasswords: true });
        return { matchPasswords: { value: control.value } };
      }

      return null;
    };
  }

  getErrorMessage(control: string) {
    const ctrl = this.form.controls[control];

    if (ctrl.hasError('required')) {
      return 'You must enter a value';
    }

    if (ctrl.hasError('email')) {
      return 'Not a valid email';
    }

    if (null !== ctrl.errors) {
      if (ctrl.hasError('maxlength')) {
        return `Maximum length is ${ctrl.errors['maxlength'].requiredLength} characters`;
      }

      if (ctrl.hasError('minlength')) {
        return `Minimum length is ${ctrl.errors['minlength'].requiredLength} characters`;
      }
    }

    if (ctrl.hasError('matchPasswords')) {
      return 'Passwords must match';
    }

    return 'Wrong value';
  }

  async register() {
    try {
      this.error = undefined;

      const payload = { ...this.form.value };
      delete payload.confirmPassword;

      this.spinner.show('Signing up');

      const result = await rpc('identity', 'accounts', 'Register', payload, false);
      setLoginInfo(result);
      this.router.navigateByUrl('/main');
    }
    catch (e: any) {
      this.error = e;
    }

    this.spinner.hide();
  }
}
