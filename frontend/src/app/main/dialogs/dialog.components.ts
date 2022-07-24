/** Application dialog components */

import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormControl, Validators } from '@angular/forms';
import { StatusResponse } from '../users/users.component';
import { Role, Privilege } from '../roles/roles.component';
import { Order, Product } from '../orders/orders.component';
import { UserInfo, rpc, getTenants, Tenant, getUserInfo } from '../../../utils';
import { addError } from '../errors';

@Component({
  templateUrl: 'dialog-confirm.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogConfirm {
  constructor(public dialogRef: MatDialogRef<DialogConfirm>,
    @Inject(MAT_DIALOG_DATA) public data: { title: string, message: string }) {
  }
  onYes(): void {
    this.dialogRef.close('yes');
  }
}

@Component({
  templateUrl: 'dialog-add-user.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogAddUser {
  error: any;
  lastResult?: StatusResponse;
  name: string = '';

  constructor(@Inject(MAT_DIALOG_DATA) public data: { onAddUser: Function }, public dialogRef: MatDialogRef<DialogAddUser>) { }

  async addUser() {
    this.lastResult = undefined;
    this.error = undefined;

    try {
      this.lastResult = await this.data.onAddUser(this.name);
    }
    catch (e) {
      this.error = e;
    }
  }
}

@Component({
  templateUrl: 'dialog-user.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogUser {
  nameFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);
  emailFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);
  firstNameFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);
  lastNameFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);
  companyFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);

  error: any;
  lastResult?: UserInfo;
  currentCtrl: FormControl;
  user: UserInfo;

  isFormChanged: boolean = false;

  constructor(@Inject(MAT_DIALOG_DATA) public data: { user: UserInfo }, public dialogRef: MatDialogRef<DialogUser>) {
    this.user = JSON.parse(JSON.stringify(data.user));

    this.nameFormControl.setValue(data.user.name);
    this.emailFormControl.setValue(data.user.email);
    this.firstNameFormControl.setValue(data.user.firstName);
    this.lastNameFormControl.setValue(data.user.lastName);
    this.companyFormControl.setValue(data.user.company);

    const onValueChanges = (value: string) => {
      if (this.isChanged(data.user))
        this.isFormChanged = true;
      else
        this.isFormChanged = false;
      return value;
    };

    this.nameFormControl.valueChanges.subscribe(onValueChanges);
    this.emailFormControl.valueChanges.subscribe(onValueChanges);
    this.firstNameFormControl.valueChanges.subscribe(onValueChanges);
    this.lastNameFormControl.valueChanges.subscribe(onValueChanges);
    this.companyFormControl.valueChanges.subscribe(onValueChanges);
  }

  getErrorMessage(control: string) {
    switch (control) {
      case "nameFormControl":
        this.currentCtrl = this.nameFormControl;
        break;
      case "emailFormControl":
        this.currentCtrl = this.emailFormControl;
        break;
      case "firstNameFormControl":
        this.currentCtrl = this.firstNameFormControl;
        break;
      case "lastNameFormControl":
        this.currentCtrl = this.lastNameFormControl;
        break;
      case "companyFormControl":
        this.currentCtrl = this.companyFormControl;
        break;
    }

    if (this.currentCtrl.hasError('required')) {
      return 'You must enter a value';
    }

    if (this.currentCtrl.errors != null) {
      if (this.currentCtrl.hasError('maxlength')) {
        return `Maximum length is ${this.currentCtrl.errors['maxlength'].requiredLength} characters`;
      }

      if (this.currentCtrl.hasError('minlength')) {
        return `Minimum length is ${this.currentCtrl.errors['minlength'].requiredLength} characters`;
      }
    }
    return 'Wrong value';
  }

  isChanged(user: UserInfo): boolean {
    if (this.nameFormControl.value != user.name || this.emailFormControl.value != user.email ||
      this.firstNameFormControl.value != user.firstName || this.lastNameFormControl.value != user.lastName ||
      this.companyFormControl.value != user.company) {
      return true;
    }
    else {
      return false;
    }
  }

  isValid(): boolean {
    return this.nameFormControl.valid && this.emailFormControl.valid && this.firstNameFormControl.valid &&
      this.lastNameFormControl.valid && this.companyFormControl.valid && this.isFormChanged;
  }

  async saveUser() {
    this.lastResult = undefined;
    this.error = undefined;

    try {
      this.user.name = this.nameFormControl.value;
      this.user.email = this.emailFormControl.value;
      this.user.firstName = this.firstNameFormControl.value;
      this.user.lastName = this.lastNameFormControl.value;
      this.user.company = this.companyFormControl.value;

      const info = await rpc('identity', 'manage', 'Update', this.user);

      if (this.error == undefined) {
        this.dialogRef.close(info);
      }
    }
    catch (e) {
      this.error = e;
    }
  }
}

@Component({
  templateUrl: 'dialog-user-roles.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogUserRoles {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { selected: boolean[], roles: Role[] }, public dialogRef: MatDialogRef<DialogUserRoles>) {
  }

  async saveRoles() {
    this.dialogRef.close(this.data.selected);
  }
}

@Component({
  templateUrl: 'dialog-add-role.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogAddRole {
  nameFormControl = new FormControl('', [Validators.minLength(4), Validators.maxLength(50), Validators.required]);

  constructor(@Inject(MAT_DIALOG_DATA) public data: { privileges: Privilege[] }, public dialogRef: MatDialogRef<DialogAddRole>) {
  }

  selected: boolean[] = new Array(this.data.privileges.length);

  getNameErrorMessage() {
    const ctrl = this.nameFormControl;

    if (ctrl.hasError('required')) {
      return 'You must enter a value';
    }

    if (null !== ctrl.errors && ctrl.hasError('maxlength')) {
      return `Maximum length is ${ctrl.errors['maxlength'].requiredLength} characters`;
    }

    if (null !== ctrl.errors && ctrl.hasError('minlength')) {
      return `Minimum length is ${ctrl.errors['minlength'].requiredLength} characters`;
    }

    return 'Wrong value';
  }

  addRole(): void {
    if (!this.nameFormControl.valid) {
      return;
    }

    const role: Role = {
      id: 0,
      name: this.nameFormControl.value,
      privileges: []
    };

    this.selected.forEach((v, i) => {
      if (v) {
        role.privileges.push({
          id: this.data.privileges[i].id,
          name: this.data.privileges[i].name,
        })
      }
    });

    this.dialogRef.close(role);
  }

  isValid(): boolean {
    return this.nameFormControl.valid && this.selected.includes(true);
  }
}

@Component({
  templateUrl: 'dialog-order.html',
  styleUrls: ['dialog.components.css'],
})
export class DialogAddOrder implements OnInit {
  error: any;
  id: number;
  clientName: string = '';
  customer: string = '';
  comment: string = '';
  street: string = '';
  zipCode: string = '';
  countryCode: string = '';
  products: Product[] = [];
  selected: boolean[] = [];
  saving = false;
  lastResult?: Order;
  usersList: (Tenant|undefined)[] = [undefined, ...getTenants()];
  user?: Tenant;

  constructor(@Inject(MAT_DIALOG_DATA) public data: { order: Order, addOrder: Function, saveOrder: Function }, public dialogRef: MatDialogRef<DialogAddOrder>) {
    if (data.order) {
      this.id = data.order.data.id;
      this.user = this.usersList.find(t => t?.id === data.order.data.user);
      this.customer = data.order.data.customer;
      this.comment = data.order.data.comment;
      this.street = data.order.data.address.street;
      this.zipCode = data.order.data.address.zipCode;
      this.countryCode = data.order.data.address.countryCode;
      this.clientName = data.order.data.customer.toString();
      if(!this.user && data.order.data.user !== getUserInfo()?.id) {
        addError("User not found");
      }
    }
  }

  async ngOnInit() {
    try {
      const response = await rpc('orders', 'products', 'list');
      this.products = response.productList;
      this.selected = new Array(this.products.length);
      if (this.data.order) {
        this.data.order.data.orderProductList.forEach(item => {
          const index = this.products.findIndex(p => p.id === item.id)
          if (index >= 0) {
            this.selected[index] = true;
          }
        });
      }
    } catch (e) {
      this.error = e;
    }
  }

  getOrderProductList() {
    const orderProductList: Product[] = [];
    this.selected.forEach((v, i) => {
      if (v) {
        orderProductList.push({ id: this.products[i].id });
      }
    });
    return orderProductList;
  }

  async addOrder() {
    this.lastResult = undefined;
    this.error = undefined;

    const { street, zipCode, countryCode } = this;
    const address = { street, zipCode, countryCode };

    const order = {
      user: this.user?.id,
      customer: this.customer,
      comment: this.comment,
      orderProductList: this.getOrderProductList(),
      address,
    };

    this.saving = true;
    try {
      this.lastResult = await this.data.addOrder(order);
    }
    catch (e: any) {
      this.error = e.message;
    }
    finally {
      this.saving = false;
    }
  }

  async saveOrder() {
    this.lastResult = undefined;
    this.error = undefined;

    const { street, zipCode, countryCode } = this;

    const order = {
      id: this.id,
      user: this.user?.id ||  getUserInfo()?.id,
      customer: this.customer,
      comment: this.comment,
      orderProductList: this.getOrderProductList(),
      address: { street, zipCode, countryCode },
    };

    this.saving = true;
    try { 
      this.lastResult = await this.data.saveOrder(order);
      this.dialogRef.close();
    }
    catch (e) {
      this.error = e;
    }
    finally {
      this.saving = false;
    }
  }
}
