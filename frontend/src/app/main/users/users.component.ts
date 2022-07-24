import { Component, OnInit, ViewChild } from '@angular/core';
import { DialogAddUser, DialogConfirm, DialogUserRoles } from '../dialogs/dialog.components';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table'
import { Role } from '../roles/roles.component';
import { addError } from '../errors';
import { rpc } from '../../../utils';

export interface User {
  id: number;
  name: string;
  email: string;
  roles: Role[];
}

export interface StatusResponse {
  status: string;
}

@Component({
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent implements OnInit {
  displayedColumns: string[] = ['id', 'name', 'email', 'roles', 'actions'];
  roles: Role[] = [];
  dataSource: User[] = [];
  loading: boolean = false;
  dialogOptions: MatDialogConfig = {autoFocus: true, disableClose:true, restoreFocus:true};

  @ViewChild(MatTable) table?: MatTable<any>;

  constructor(public dialog: MatDialog)
  {

  }

  async ngOnInit() {
    try {
      this.loading = true;
      const rolesList = rpc('identity', 'roles', 'RolesList');

      const list = await rpc('identity','accounts','List');

      this.dataSource = list.appUsers;

      const userRoles = await rpc('identity','roles','GetUsersRoles', { userIds: this.dataSource.map(u=>u.id) });

      this.dataSource.forEach(u=> {
        const roles = userRoles.userRoles.find( (ur: any) => ur.userId === u.id)
        if( roles ) {
          u.roles = roles.roles;
        }
      });

      this.roles = (await rolesList).roles;

    } catch(e) {
      addError(e);
    }

    this.loading = false;
  }

  getUserRoles(user:User){
    return user.roles?.map(r => r.name).join(', ');
  }

  removeUser(user:User) {
    this.dialog.open(DialogConfirm, {...this.dialogOptions, data: {
      title:'Remove User',
      message:'Do you really want to remove user?',
    }}).afterClosed()
    .subscribe(async result => {
      if(result==='yes') {
        const response: StatusResponse = await rpc('identity', 'users', 'DetachUser', { id: user.id } );
        if(response.status==="Success") {
          this.dataSource = this.dataSource.filter(u=>u.id!==user.id);
        }
      }
    });
  }

  async onAddUser(name:string) {
    if(this.dataSource.some(u=>u.name === name)) {
      throw new Error(`Users with name "${name}" already added to list`);
    }

    try {
      const user = await rpc('identity','users','FindUserByName', {name});
      const response: StatusResponse = await rpc('identity', 'users', 'AttachUser', { id: user.id } );
        if(response.status==="Success") {
          this.dataSource.unshift(user);
          if(this.table) {
            this.table.renderRows();
          }
        }

      return user;

    } catch {
      throw new Error(`Users with name ${name} not found`);
    }
  }

  addUser() {
    this.dialog.open(DialogAddUser, {...this.dialogOptions, width:"400px", data: { onAddUser: (n:string) => this.onAddUser(n) }});
  }

  editRoles(user: User) {
    const selected = this.roles.map(r => user.roles ? user.roles.some(ur => r.id===ur.id) : false);

    this.dialog.open(DialogUserRoles, {...this.dialogOptions, width:"400px", data: { selected, roles: this.roles } })
      .afterClosed()
      .subscribe(async result => {
        if(typeof(result) === 'object' && result.length>0) {
          const selectedRoles: Role[] = [];
          result.forEach((r:boolean, index:number) => { if(r) {
            selectedRoles.push(this.roles[index]);
          }});

          try {
            const response = await rpc('identity','roles','AssignUserRoles', {userId: user.id, roleIds: selectedRoles.map(r=>r.id)});
            user.roles = response.roles;
          } catch(e) {
            addError(e);
          }
        }
      });
  }
}
