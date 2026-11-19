import { CountryType } from '../../types/countryType';
import React from 'react';

type CountryFlagProps = {
    country: CountryType;
};

const CountryFlag: React.FC<CountryFlagProps> = ({ country }) => (
    <span
        className='flag'
        role='img'
        title={country.name}
        aria-label={country.name}
    >
        {country.flag}
    </span>
);

export default React.memo(CountryFlag);
