import { UserService } from '../services/userService.js';

class UserController {
    constructor() {
        this.userService = new UserService();
    }

    getUser(id) {
        return this.userService.findById(id);
    }

    getAllUsers() {
        return this.userService.getAll();
    }

    createUser(name, email) {
        this.userService.save({ name, email });
    }
}

const formatResponse = (data) => {
    return { status: 'ok', data };
};

export { UserController, formatResponse };
