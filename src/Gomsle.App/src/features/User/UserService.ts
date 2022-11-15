import { FetchBase } from '../../infrastructure/FetchBase';

export interface RegisterParamters {
  email: string;
  password: string;
}

export class UserService extends FetchBase {
  baseUrl = `${process.env.REACT_APP_API_URL}/user`;

  async register(data: RegisterParamters) {
    return await this.post<User>(`${this.baseUrl}/register`, data);
  }
}
