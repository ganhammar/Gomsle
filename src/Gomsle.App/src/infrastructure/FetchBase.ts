interface Request {
  method: 'get' | 'post' | 'put' | 'delete';
  url: string;
  body?: object;
}

export abstract class FetchBase {
  protected async get<T>(url: string) {
    return await this.request<T>({ method: 'get', url });
  }

  protected async post<T>(url: string, body: object) {
    return await this.request<T>({ method: 'post', url, body });
  }

  protected async put<T>(url: string, body: object) {
    return await this.request<T>({ method: 'put', url, body });
  }

  protected async delete<T>(url: string) {
    return await this.request<T>({ method: 'delete', url });
  }

  private async request<T>({ method, url, body }: Request) {
    const headers = new Headers();
    const options: RequestInit = { method, headers };

    if (body) {
      headers.set('Content-Type', 'application/json');
      options.body = JSON.stringify(body);
    }

    const response = await fetch(url, options);

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error);
    }

    return (await response.json()) as T;
  }
}
