import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';

import { Component } from '@angular/core';
import { StatusResponse } from '../users/users.component';
import { rpc } from '../../../utils';

@Component({
  templateUrl: 'dialog-change-password.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogChangePassword {
  form: FormGroup;
  error: Error|undefined;
  lastResult?: StatusResponse;

  constructor(fb: FormBuilder) {
     this.form = fb.group({
      currentPassword: [''],
      newPassword: ['', [Validators.minLength(8), Validators.maxLength(300), Validators.required]],
      confirmPassword: [''],
    }, { validators: [this.matchPasswordsValidator()] });
  }

  matchPasswordsValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (this.form?.value.confirmPassword !== this.form?.value.newPassword) {
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

  async save() {
    try {
      this.error = undefined;
      this.lastResult = undefined;

      const payload = { ...this.form.value };
      //В rpc не допускается отправка лишних полей
      delete payload.confirmPassword;

      const result = await rpc('identity', 'manage', 'ChangePassword', payload);
      if(result && "Success" === result.status) {
        this.lastResult = result.status;
      }
    }
    catch (e: any) {
      this.error = e.message;
    }
  }
}
