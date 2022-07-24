import { Component, OnInit, ViewChild } from '@angular/core';
import { DialogAddOrder, DialogConfirm } from '../dialogs/dialog.components';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table';
import { rpc } from '../../../utils';
import { addError } from '../errors';

export interface Order {
  data: OrderData;
  createdBy: number;
  createdAt: string;
  updatedBy: number;
  updatedAt: Date;
  deletedBy: number;
  deletedAt: Date;
  combinedProducts: string;
}

export interface Product {    
  id: number;
  name?: string;
  description?: string;
}

interface OrderData {
  id: number;
  user?: number;
  customer: string;
  comment: string;
  orderProductList: Product[];
  address: Address;
}

interface Address {    
  id?: number;
  street: string;
  zipCode: string;
  countryCode: string;
}

@Component({
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.scss']
})
export class OrdersComponent implements OnInit {
  displayedColumns: string[] = ['id', 'user', 'createdBy', 'customer', 'createdAt', 'combinedProducts', 'actions'];

  ordersDataSource: Order[];

  dialogOptions: MatDialogConfig = { autoFocus: true, disableClose: true, restoreFocus: true };

  @ViewChild(MatTable) table?: MatTable<any>;

  loading: boolean = false;

  constructor(public dialog: MatDialog) {
  }

  async ngOnInit() {
    try {
      this.loading = true;
      const response = await rpc('orders', 'orders', 'list');

      const orders = response.ordersList as Order[];

      for (const o of orders) {
        this.updateOrderFields(o);
      }

      this.ordersDataSource = orders;
    } catch (e) {
      addError(e);
    }

    this.loading = false;
  }

  updateOrderFields(o: Order){
    o.combinedProducts = this.getShortenItems(o.data.orderProductList);
    o.createdAt = new Intl.DateTimeFormat([], { dateStyle: 'short', timeStyle: 'short' }).format(new Date(o.createdAt))
  }

  getShortenItems(products: Product[]) {
    const maxLength = 100;
    let result = '';

    for (let i = 0; i < products.length && result.length < maxLength; i++) {
      if (products[i].name) {

        if (result.length > 0) {
          result += ', ';
        }

        result += products[i].name;
      }
    }
    return result.length >= maxLength ? result.slice(0, maxLength) + '...' : result;
  }

  async onAddOrder(payload: object) {
    const response: Order = await rpc('orders', 'orders', 'create', payload);
    if (response.data?.id) {
      this.updateOrderFields(response);
      this.ordersDataSource.unshift(response);
      if (this.table) {
        this.table.renderRows();
      }
    }

    return response;
  }

  async removeOrder(order: Order) {
    this.dialog.open(DialogConfirm, {
      ...this.dialogOptions, data: {
        title: 'Remove Order',
        message: 'Do you really want to remove orders?',
      }
    }).afterClosed()
      .subscribe(async result => {
        if (result === 'yes') {
          try {
            const response: Order = await rpc('orders', 'orders', 'delete', { id: order.data.id });
            if (response.data?.id) {
              this.ordersDataSource = this.ordersDataSource.filter(o => o.data.id !== response.data.id);
            }
          } catch (e) {
            addError(e);
          }
        }
      });
  }

  async onSaveOrder(payload: object) {
    const response: Order = await rpc('orders', 'orders', 'update', payload);
    if (response.data?.id) {
      this.updateOrderFields(response);
      const index = this.ordersDataSource.findIndex(o => o.data.id === response.data.id);
      this.ordersDataSource[index] = response;
      if (this.table) {
        this.table.renderRows();
      }
    }

    return response;
  }

  editOrder(order: Order) {
    this.dialog.open(DialogAddOrder, { ...this.dialogOptions, width: '400px', data: { order, saveOrder: (payload: object) => this.onSaveOrder(payload) } });
  }

  addOrder() {
    this.dialog.open(DialogAddOrder, { ...this.dialogOptions, width: '400px', data: { addOrder: (payload: object) => this.onAddOrder(payload) } });
  }
}
