import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { UserInfo, isAdmin, isValidAccessToken, removeRefreshToken, rpc, setTenants, setUserInfo } from '../../utils';
import { removeError, setNotify } from './errors';

import { DialogChangePassword } from './dialogs/dialog-change-password';
import { DialogUser } from './dialogs/dialog.components';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { SpinnerOverlayService } from '../services/spinner';

@Component({
  selector: 'app-main-root',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {

  dialogOptions: MatDialogConfig = {autoFocus: true, disableClose:true, restoreFocus:true};

  constructor(private router: Router,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private spinner: SpinnerOverlayService) {
  }

  error: string|null = null;
  userInfo: UserInfo;
  isAdmin = isAdmin;
  hasTenants = false;

  async ngOnInit() {
    if (!isValidAccessToken()) {
      this.router.navigate(['/login']);
    }

    try {
      const tInfo = rpc('identity', 'manage', 'Info');
      const tTenants = rpc('identity', 'manage', 'GetTenants');
      this.userInfo = await tInfo;
      setUserInfo(this.userInfo);
      const tenants = (await tTenants).tenants;
      setTenants(tenants);
      this.hasTenants = Array.isArray(tenants) && tenants.length>0
    } catch {

    }

    setNotify(this.notify.bind(this));
    this.displayErrors();
  }

  notify(err: string) {
    this.error = err;
    this.displayErrors();
  }

  displayErrors() {
    if(null !== this.error) {
      this.snackBar.open(this.error, 'Close',
      {
        panelClass: ['error-snackbar'],
        horizontalPosition: 'center',
        verticalPosition: 'bottom',
      }).afterDismissed().subscribe(() => {
        removeError(this.error);
        setTimeout(()=>this.displayErrors(), 200);
      });
    }
  }

  async logout() {
    try {
      this.spinner.show('Signing out');
      const result = await rpc('identity','accounts','Logout');
      if(result && "Success" === result.status) {
        removeRefreshToken();
        this.router.navigateByUrl('/login');
      }
    }
    catch (e) {
      alert("Logout failed! " + e);
    }

    this.spinner.hide();
  }

  changePassword() {
    this.dialog.open(DialogChangePassword, {...this.dialogOptions, width:"400px"} );
  }

  manageUser() {
    this.dialog.open(DialogUser, {...this.dialogOptions, width:"400px", data: {user: this.userInfo }} )
    .afterClosed()
    .subscribe( async (user: UserInfo) => {
      if(typeof(user) === 'object') {
        this.userInfo = user;
        setUserInfo(this.userInfo);
      }
    });
  }
}
