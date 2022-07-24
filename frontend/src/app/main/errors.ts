let errors: string[] = [];
let notify : Function;

export function setNotify(func: Function) {
    notify = func;
}

export function addError(error: any) {
    errors.push(error);
    if(notify) {
        notify(error);
    }
}

export function removeError(error: any) {
    const index = errors.lastIndexOf(error);
    if(index > -1) {
        errors.splice(index, 1);
    }

    if(notify && errors.length>0) {
        notify(errors[errors.length-1]);
    } else {
        notify(null);
    }
}