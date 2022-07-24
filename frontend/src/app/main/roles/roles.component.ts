import { Component, OnInit, ViewChild } from '@angular/core';
import { DialogAddRole, DialogConfirm } from '../dialogs/dialog.components';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table'
import { addError } from '../errors';
import { rpc } from '../../../utils';

export interface Role {
  id: number;
  name: string;
  privileges: Privilege[];
}

export interface Privilege {
  id: number;
  name: string;
}

@Component({
  templateUrl: './roles.component.html',
  styleUrls: ['./roles.component.scss']
})
export class RolesComponent implements OnInit {
  displayedColumns: string[] = ['id', 'name', 'privileges', 'actions'];
  dataSource: Role[] = [];
  privileges: Privilege[] = [];
  loading: boolean = false;
  dialogOptions: MatDialogConfig = {autoFocus: true, disableClose:true, restoreFocus:true};

  @ViewChild(MatTable) table?: MatTable<any>;

  constructor(public dialog: MatDialog)
  {
  }

  async ngOnInit() {

    try {
      this.loading = true;
      const tPrivs = rpc('identity', 'roles', 'PrivilegesList');

      this.dataSource = (await rpc('identity', 'roles', 'RolesList')).roles;
      this.privileges = (await tPrivs).privileges;
    } catch(e) {
      addError(e);
    }

    this.loading = false;
  }

  deleteRole(id:number) {
    this.dialog.open(DialogConfirm, {...this.dialogOptions, data: {
      title:'Remove User',
      message:'Do you really want to delete role?',
    }}).afterClosed()
    .subscribe(async result => {
      if(result==='yes') {
        try {
          const result:Role = await rpc('identity', 'roles', 'RoleRemove', {roleId: id} );
          this.dataSource = this.dataSource.filter(r => r.id !== result.id);
        } catch(e) {
          addError(e);
        }
      }
    });
  }

  getRolePrivileges(r: Role) {
    let privileges = '';

    for(let i=0; i<r.privileges.length; ++i) {
      const roleName: string = r.privileges[i].name;

      if(!roleName) {
         continue;
      }

      if(privileges.length + roleName.length > 200) {
        privileges += ' ...'
        break;
      }

      if(privileges) {
        privileges += ', ';
      }

      privileges += roleName;
    }

    return privileges;
  }

  addRole() {
    this.dialog.open(DialogAddRole, {...this.dialogOptions, width:"400px", data: {privileges: this.privileges}} )
    .afterClosed()
    .subscribe( async (role:Role) => {
      if(typeof(role) === 'object') {

        try {
          const result: Role = await rpc('identity', 'roles', 'RoleCreate', role );
          //Имена привилегий не возвращаются в запросе RoleCreate
          result.privileges.forEach(rp => rp.name = this.privileges.find(p=>p.id===rp.id)?.name || '' )

          this.dataSource.unshift(result);
          if(this.table) {
            this.table.renderRows();
          }

          //this.dataSource = [result, ...this.dataSource];
        } catch(e) {
          addError(e);
        }
      }
    });
  }
}
