import { useState, useCallback } from 'react';

const useAsyncError = () => {
  const [, setError] = useState();

  return useCallback(
    (error: any) => {
      setError(() => {
        throw error;
      });
    },
    [setError]
  );
};

export default useAsyncError;
