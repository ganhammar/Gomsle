import { ReactNode } from 'react';
import styled from 'styled-components';

type colors = 'primary' | 'secondary' | 'warning' | 'success' | 'divider';

interface ElementProps {
  color: colors;
  isDisabled: boolean;
  isLoading: boolean;
}

const Element = styled.button<ElementProps>`
  background-color: ${({ theme, color, isDisabled }) =>
    isDisabled ? theme.palette.divider.main : theme.palette[color].main};
  color: ${({ theme, color, isDisabled }) =>
    isDisabled
      ? theme.palette.divider.contrastText
      : theme.palette[color].contrastText};
  border: none;
  padding: ${({ theme }) => theme.spacing.xs} ${({ theme }) => theme.spacing.m};
  border-radius: ${({ theme }) => theme.borderRadius};
  box-shadow: ${({ theme, isDisabled }) =>
    isDisabled ? theme.shadows[0] : theme.shadows[1]};
  cursor: ${({ isDisabled }) => isDisabled ? 'not-allowed' : 'pointer'};
  transition: box-shadow 0.5s, opacity 0.5s;
  &:hover {
    box-shadow: ${({ theme }) => theme.shadows[0]};
    opacity: 0.9;
  }
  ${({ isLoading }) => isLoading && `
    opacity: 0;
    cursor: auto;
    &:hover {
      opacity: 0;
    }
  `}
`;

interface Props {
  children: ReactNode;
  color: colors;
  className?: string;
  isDisabled?: boolean;
  isLoading?: boolean;
  onClick: () => void;
}

export default function Button({
  children,
  className,
  color,
  isDisabled,
  isLoading,
  onClick,
}: Props) {
  const submit = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();

    if (isDisabled === false && isLoading === false) {
      onClick();
    }
  };

  return (
    <Element
      isDisabled={isDisabled ?? false}
      color={color}
      className={className}
      onClick={submit}
      isLoading={isLoading ?? false}
    >
      {children}
    </Element>
  );
}
