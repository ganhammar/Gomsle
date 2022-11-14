import { useState } from 'react';
import styled from 'styled-components';
import Button from '../../components/Button';
import TextInput from '../../components/TextInput';
import isEmail from '../../utils/isEmail';

const MIN_PASSWORD_LENGTH = 8;

const Form = styled.form`
  display: flex;
  flex-direction: column;
`;
const Submit = styled(Button)`
  margin-left: auto;
  margin-top: 0.5rem;
`;

export function Register() {
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');

  const submit = () => {};

  return (
    <Form>
      <TextInput
        title="Email"
        type="text"
        value={email}
        onChange={setEmail}
        hasError={Boolean(email) && !isEmail(email)}
        errorTip="Must be a valid email address"
      />
      <TextInput
        title="Password"
        type="password"
        value={password}
        onChange={setPassword}
        hasError={Boolean(password) && password.length < MIN_PASSWORD_LENGTH}
        errorTip="At least eight characters"
      />
      <Submit
        color="success"
        onClick={submit}
        isDisabled={!isEmail(email) || password.length < MIN_PASSWORD_LENGTH}
      >
        Register
      </Submit>
    </Form>
  );
}
